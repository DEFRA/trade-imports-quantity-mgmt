using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TradeImportsQuantityMgmt.Features.QuantityManagement;

namespace TradeImportsQuantityMgmt.Tests;

public record OutcomeMeasurement(string Operation, string Outcome, string StatusCode);

/// <summary>
/// A <see cref="QuantityManagementOutcomeRecorder"/> wired to a substitute logger and a private
/// meter, capturing the outcome measurements it emits.
/// </summary>
public sealed class QuantityManagementOutcomeCapture : IDisposable
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly MeterListener _listener = new();
    private readonly List<OutcomeMeasurement> _measurements = [];

    public QuantityManagementOutcomeCapture()
    {
        Logger = Substitute.For<ILogger<QuantityManagementOutcomeRecorder>>();
        Recorder = new QuantityManagementOutcomeRecorder(Logger, new QuantityManagementMetrics(_meterFactory, "Test"));

        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (_meterFactory.Meters.Contains(instrument.Meter))
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>(
            (_, _, tags, _) =>
            {
                var values = tags.ToArray().ToDictionary(x => x.Key, x => x.Value?.ToString());
                lock (_measurements)
                {
                    _measurements.Add(
                        new OutcomeMeasurement(
                            values[QuantityManagementMetrics.Constants.Tags.Operation]!,
                            values[QuantityManagementMetrics.Constants.Tags.Outcome]!,
                            values[QuantityManagementMetrics.Constants.Tags.StatusCode]!
                        )
                    );
                }
            }
        );
        _listener.Start();
    }

    public ILogger<QuantityManagementOutcomeRecorder> Logger { get; }

    public QuantityManagementOutcomeRecorder Recorder { get; }

    public IReadOnlyList<OutcomeMeasurement> Measurements
    {
        get
        {
            lock (_measurements)
                return _measurements.ToList();
        }
    }

    public void Dispose()
    {
        _listener.Dispose();
        _meterFactory.Dispose();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public List<Meter> Meters { get; } = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
            Meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in Meters)
                meter.Dispose();
        }
    }
}
