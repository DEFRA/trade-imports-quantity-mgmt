using AwesomeAssertions;
using Defra.TradeImports.Tracing;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using TradeImportsQuantityMgmt.Utils.Logging;

namespace TradeImportsQuantityMgmt.Tests.Utils.Logging;

public class TraceIdEnricherTests
{
    private readonly MessageTemplateParser _parser = new();

    private LogEvent CreateLogEvent() =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            _parser.Parse("msg"),
            Enumerable.Empty<LogEventProperty>()
        );

    [Fact]
    public void Enrich_DoesNotAddProperty_WhenContextIsNull()
    {
        var accessor = Substitute.For<ITraceContextAccessor>();
        accessor.Context.Returns((TraceContext?)null);

        var enricher = new TraceIdEnricher("x-correlation-id", addValueIfHeaderAbsence: false, accessor);
        var evt = CreateLogEvent();
        var factory = Substitute.For<ILogEventPropertyFactory>();

        enricher.Enrich(evt, factory);

        evt.Properties.ContainsKey("CorrelationId").Should().BeFalse();
    }

    [Fact]
    public void Enrich_AddsTraceId_WhenContextHasTraceId()
    {
        var accessor = Substitute.For<ITraceContextAccessor>();
        accessor.Context.Returns(new TraceContext { TraceId = "trace-123" });

        var enricher = new TraceIdEnricher("x-correlation-id", addValueIfHeaderAbsence: false, accessor);
        var evt = CreateLogEvent();
        var factory = Substitute.For<ILogEventPropertyFactory>();

        enricher.Enrich(evt, factory);

        evt.Properties.ContainsKey("CorrelationId").Should().BeTrue();
        var prop = evt.Properties["CorrelationId"];
        prop.Should().BeOfType<ScalarValue>();
        ((ScalarValue)prop).Value.Should().Be("trace-123");
    }

    [Fact]
    public void Enrich_GeneratesGuid_WhenTraceIdMissing_AndAddValueAllowed()
    {
        var accessor = Substitute.For<ITraceContextAccessor>();
        accessor.Context.Returns(new TraceContext { TraceId = string.Empty });

        var enricher = new TraceIdEnricher("x-correlation-id", addValueIfHeaderAbsence: true, accessor);
        var evt = CreateLogEvent();
        var factory = Substitute.For<ILogEventPropertyFactory>();

        enricher.Enrich(evt, factory);

        evt.Properties.ContainsKey("CorrelationId").Should().BeTrue();
        var prop = evt.Properties["CorrelationId"];
        prop.Should().BeOfType<ScalarValue>();
        var value = ((ScalarValue)prop).Value as string;
        value.Should().NotBeNull();
        value!.Length.Should().Be(32); // GUID in "N" format
    }
}
