using System.Diagnostics.CodeAnalysis;
using Defra.TradeImports.Api.Metrics;
using Defra.TradeImports.EmfExporter;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.AWS;
using Serilog;
using TradeImportsQuantityMgmt.Config;
using TradeImportsQuantityMgmt.Example.Endpoints;
using TradeImportsQuantityMgmt.Example.Services;
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

    // Trust material must be loaded before anything creates outbound connections.
    services.LoadCustomTrustStoreFromEnvironment();

    services.AddProblemDetails();
    services.AddValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Trade Imports Quantity Management", Version = "v1" });
    });

    // Default HTTP Client
    builder.Services.AddHttpClient("DefaultClient").AddHeaderPropagation();

    // Proxy HTTP Client
    builder.Services.AddTransient<ProxyHttpMessageHandler>();
    builder.Services.AddHttpClient("proxy").ConfigurePrimaryHttpMessageHandler<ProxyHttpMessageHandler>();

    builder.Services.AddApiMetrics();

    services.AddHttpContextAccessor();

    ConfigureHeaderPropagation(services, configuration);
    ConfigureHttpClients(services);
    ConfigureMongo(services, configuration);

    services.AddHealthChecks();

    // App services
    services.AddSingleton<IExamplePersistence, ExamplePersistence>();
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
static void ConfigureHttpClients(IServiceCollection services)
{
    services.AddTransient<ProxyHttpMessageHandler>();
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
    app.MapHealthChecks("/health").AllowAnonymous();
    app.UseEmfExporter(Constants.MeterName);
}

[ExcludeFromCodeCoverage]
static void ConfigureEndpoints(WebApplication app)
{
    app.MapHealthChecks("/health", new HealthCheckOptions());

    // Remove before deploying
    app.MapExampleEndpoints();
}
