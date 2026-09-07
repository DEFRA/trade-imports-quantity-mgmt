using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TradeImportsQuantityMgmt.Health;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddTracesGateway(timeout: TimeSpan.FromSeconds(10), tags: [WebApplicationExtensions.Extended]);
    }
}
