using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
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

    public static IHealthChecksBuilder AddSqs(
        this IHealthChecksBuilder builder,
        string queueUrl,
        Func<IServiceProvider, string> queueNameFunc,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null
    )
    {
        builder.Add(
            new HealthCheckRegistration(
                queueUrl,
                sp => new SqsHealthCheck(sp.GetRequiredService<IAmazonSQS>(), queueNameFunc(sp)),
                HealthStatus.Unhealthy,
                tags,
                timeout
            )
        );

        return builder;
    }
}
