using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Defra.TradeImports.Api.Metrics;
using Defra.TradeImports.EmfExporter;
using Defra.TradeImports.Tracing;
using FluentValidation;
using Infrastructure;
using Infrastructure.Messaging.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using Trade.Gateway.Api.Client.Extensions;
using TradeImportsQuantityMgmt.Config;
using TradeImportsQuantityMgmt.Endpoints;
using TradeImportsQuantityMgmt.Features.ResourceEvents;
using TradeImportsQuantityMgmt.Health;
using TradeImportsQuantityMgmt.Utils;
using TradeImportsQuantityMgmt.Utils.Http;
using TradeImportsQuantityMgmt.Utils.Logging;
using TradeImportsQuantityMgmt.Utils.Mongo;

var app = BuildApp(args);
await app.RunAsync();

[ExcludeFromCodeCoverage]
static WebApplication BuildApp(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    ConfigureHost(builder);
    ConfigureServices(builder);

    var app = builder.Build();

    ConfigureMiddleware(app);
    ConfigureEndpoints(app);

    return app;
}

[ExcludeFromCodeCoverage]
static void ConfigureHost(WebApplicationBuilder builder)
{
    builder.Host.UseSerilog(CdpLogging.Configuration);
}

[ExcludeFromCodeCoverage]
static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;

    services.Configure<JsonOptions>(opts => opts.SerializerOptions.ConfigureJsonSerializerOptions());

    // Trust material must be loaded before anything creates outbound connections.
    services.LoadCustomTrustStoreFromEnvironment();

    services.AddProblemDetails();

    services.AddValidation();

    services
        .AddOptions<ResourceEventConsumerOptions>()
        .Bind(configuration.GetSection(ResourceEventConsumerOptions.SectionName))
        .ValidateOnStart();
    services.AddMessaging(configuration);

    services.AddConsumer<FinalisationConsumer>(
        sp => sp.GetRequiredService<IOptions<ResourceEventConsumerOptions>>().Value.ResourceEventsQueueUrl,
        message => message.IsFinalisation()
    );

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Trade Imports Quantity Management", Version = "v1" });
    });

    builder.Services.AddApiMetrics();

    services.AddHttpContextAccessor();
    services.AddTraceContextAccessor(configuration);

    ConfigureHeaderPropagation(services, configuration);
    ConfigureHttpClients(services, configuration);
    ConfigureMongo(services, configuration);

    services.AddHealth(configuration);
}

[ExcludeFromCodeCoverage]
static void ConfigureHeaderPropagation(IServiceCollection services, IConfiguration configuration)
{
    var traceHeader = configuration.GetValue<string>("TraceHeader");

    services.AddHeaderPropagation(options =>
    {
        if (!string.IsNullOrWhiteSpace(traceHeader))
        {
            options.Headers.Add(traceHeader);
        }
    });
}

[ExcludeFromCodeCoverage]
static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
{
    services
        .AddTracesGatewayApiClients(configuration)
        .WithSts()
        .WithLogging()
        .WithAcceptLanguage()
        .WithTracing(sp =>
        {
            var traceContextAccessor = sp.GetRequiredService<ITraceContextAccessor>();
            return traceContextAccessor.Context?.TraceId ?? Guid.CreateVersion7().ToString("N");
        });

    // Default HTTP Client
    services.AddHttpClient("DefaultClient").AddHeaderPropagation();

    // Proxy HTTP Client
    services.AddTransient<ProxyHttpMessageHandler>();
    services.AddHttpClient("proxy").ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();
}

[ExcludeFromCodeCoverage]
static void ConfigureMongo(IServiceCollection services, IConfiguration configuration)
{
    MongoExtensions.Register();
    MongoConventions.Register();

    services
        .AddOptions<MongoConfig>()
        .Bind(configuration.GetRequiredSection("Mongo"))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    services.AddSingleton<IMongoDbClientFactory, MongoDbClientFactory>();
}

[ExcludeFromCodeCoverage]
static void ConfigureMiddleware(WebApplication app)
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = ".well-known/openapi/{documentName}/openapi.json";
    });
    app.UseReDoc(options =>
    {
        options.RoutePrefix = "redoc";
        options.ConfigObject.ExpandResponses = "200";
        options.SpecUrl("/.well-known/openapi/v1/openapi.json");
    });

    app.UseMiddleware<ApiMetricsMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHeaderPropagation();
    app.MapHealth();
    app.UseEmfExporter(app.Services.GetRequiredService<IOptions<ApiMetricsOptions>>().Value.MeterName);
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplication app)
{
    app.UseChedQuantityEndpoints();
}
