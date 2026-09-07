using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trade.Gateway.Api.Client.Clients;

namespace TradeImportsQuantityMgmt.Health;

[ExcludeFromCodeCoverage]
public static class HealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddTracesGateway(
        this IHealthChecksBuilder builder,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null
    )
    {
        builder.Add(
            new HealthCheckRegistration(
                "Traces Gateway",
                sp => new TracesGatewayHealthCheck(sp.GetRequiredService<ITracesGatewayClient>()),
                HealthStatus.Unhealthy,
                tags,
                timeout
            )
        );

        return builder;
    }
}
