using Defra.TradeImports.Tracing;
using Serilog.Core;
using Serilog.Events;

namespace TradeImportsQuantityMgmt.Utils.Logging
{
    public class TraceIdEnricher : ILogEventEnricher
    {
        private const string PropertyName = "CorrelationId";
        private readonly bool _addValueIfHeaderAbsence;
        private readonly ITraceContextAccessor _contextAccessor;

        internal TraceIdEnricher(string headerKey, bool addValueIfHeaderAbsence, ITraceContextAccessor contextAccessor)
        {
            _addValueIfHeaderAbsence = addValueIfHeaderAbsence;
            _contextAccessor = contextAccessor;
        }

        /// <inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var context = _contextAccessor.Context;
            if (context == null)
                return;

            var correlationId = string.Empty;

            if (!string.IsNullOrWhiteSpace(context.TraceId))
                correlationId = context.TraceId;
            else if (_addValueIfHeaderAbsence)
                correlationId = Guid.NewGuid().ToString("N");

            LogEventProperty correlationIdProperty = new(PropertyName, new ScalarValue(correlationId));
            logEvent.AddOrUpdateProperty(correlationIdProperty);
        }
    }
}
