using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TradeImportsQuantityMgmt.Config;

namespace TradeImportsQuantityMgmt.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqs(
                "SQS - Resource Events",
                sp => sp.GetRequiredService<IOptions<ResourceEventConsumerOptions>>().Value.ResourceEventsQueueUrl,
                timeout: TimeSpan.FromSeconds(10),
                tags: [WebApplicationExtensions.Extended]
            )
            .AddTracesGateway(timeout: TimeSpan.FromSeconds(10), tags: [WebApplicationExtensions.Extended])
            .AddDataApi(
                sp => sp.GetRequiredService<IOptions<DataApiOptions>>().Value,
                tags: [WebApplicationExtensions.Extended],
                timeout: TimeSpan.FromSeconds(10)
            );
    }
}
