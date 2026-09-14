using System.Diagnostics.CodeAnalysis;

namespace Infrastructure;

[ExcludeFromCodeCoverage]
public static class MetricNames
{
    public const string MeterName = "Trade.Imports.Quantity.Mgmt";
    public const string TraceKey = "x-cdp-request-id";
}
