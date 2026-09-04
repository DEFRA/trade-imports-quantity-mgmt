using System.Diagnostics.CodeAnalysis;
using Amazon.SecurityToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeImportsQuantityMgmt.Client.Clients;
using TradeImportsQuantityMgmt.Client.DelegatingHandlers;

namespace TradeImportsQuantityMgmt.Client.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static QuantityManagementClientsBuilder AddQuantityManagementClients(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<QuantityManagementClientOptions>()
            .Bind(configuration.GetSection(QuantityManagementClientOptions.SectionName))
            .ValidateOnStart();

        services.ConfigureHttpClientDefaults(http =>
        {
            http.RedactLoggedHeaders(_ => false);
        });

        var builder = new QuantityManagementClientsBuilder(services);

        builder.AddClient<IQuantityManagementClient>();

        return builder;
    }

    public static QuantityManagementClientsBuilder WithSts(this QuantityManagementClientsBuilder builder)
    {
        builder.Services.AddSingleton<IAmazonSecurityTokenService>(_ => new AmazonSecurityTokenServiceClient());

        builder.AddHandler<StsAuthDelegatingHandler>();

        return builder;
    }

    public static QuantityManagementClientsBuilder WithTracing(
        this QuantityManagementClientsBuilder builder,
        Func<IServiceProvider, string> traceIdAccessor
    )
    {
        builder.AddHandler<TracingDelegatingHandler>(sp => new TracingDelegatingHandler(traceIdAccessor, sp));

        return builder;
    }

    public static QuantityManagementClientsBuilder WithLogging(this QuantityManagementClientsBuilder builder)
    {
        builder.AddHandler<HttpLoggingDelegatingHandler>();

        return builder;
    }

    public static QuantityManagementClientsBuilder WithAcceptLanguage(this QuantityManagementClientsBuilder builder)
    {
        builder.AddHandler<AcceptLanguageDelegatingHandle>();

        return builder;
    }
}
