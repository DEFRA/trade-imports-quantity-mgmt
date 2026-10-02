using System.Diagnostics.CodeAnalysis;

namespace Infrastructure;

[ExcludeFromCodeCoverage]
public static class MetricNames
{
    // Must match ApiMetrics:MeterName, as that is the only meter the EMF exporter publishes.
    public const string MeterName = "Defra.Trade.Imports.Quantity.Mgmt";
    public const string TraceKey = "x-cdp-request-id";
}
