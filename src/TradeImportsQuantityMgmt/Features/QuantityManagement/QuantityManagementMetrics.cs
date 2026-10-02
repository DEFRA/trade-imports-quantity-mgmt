using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TradeImportsQuantityMgmt.Features.QuantityManagement;

public class QuantityManagementMetrics
{
    // Every measurement carries the same tags, so a reason is always present on the metric.
    public const string NoReason = "None";
    public const string UnknownReason = "Unknown";

    private readonly Counter<long> _outcomeTotal;

    public QuantityManagementMetrics(IMeterFactory meterFactory, string meterName)
    {
        var meter = meterFactory.Create(meterName);

        _outcomeTotal = meter.CreateCounter<long>(
            "QuantityManagementOutcome",
            "COUNT",
            description: "Number of Quantity Management outcome responses received from TRACES"
        );
    }

    public void Outcome(QuantityManagementOutcome outcome)
    {
        var tagList = new TagList
        {
            { Constants.Tags.Service, Process.GetCurrentProcess().ProcessName },
            { Constants.Tags.Operation, outcome.Operation.ToString() },
            { Constants.Tags.Outcome, outcome.Outcome },
            { Constants.Tags.Reason, outcome.Reason ?? (outcome.IsSuccess ? NoReason : UnknownReason) },
            { Constants.Tags.StatusCode, ((int)outcome.StatusCode).ToString() },
        };

        _outcomeTotal.Add(1, tagList);
    }

    public static class Constants
    {
        public static class Tags
        {
            public const string Service = "ServiceName";
            public const string Operation = "Operation";
            public const string Outcome = "Outcome";
            public const string Reason = "Reason";
            public const string StatusCode = "StatusCode";
        }
    }
}
