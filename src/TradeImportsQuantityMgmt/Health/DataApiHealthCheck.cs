using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TradeImportsQuantityMgmt.Config;

namespace TradeImportsQuantityMgmt.Health;

[ExcludeFromCodeCoverage]
public class DataApiHealthCheck(DataApiOptions options) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            // This code intentionally does not use the data API client as that includes
            // header propagation and other concerns that we don't want

            using var httpClient = new HttpClient();

            options.Configure(httpClient);

            var response = await httpClient.GetAsync("/health/authorized", cancellationToken);

            response.EnsureSuccessStatusCode();

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                exception: new Exception("Failed to connect to Data API", ex)
            );
        }
    }
}
