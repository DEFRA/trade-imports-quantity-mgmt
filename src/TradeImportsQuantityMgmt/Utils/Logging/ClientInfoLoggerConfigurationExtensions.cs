using Defra.TradeImports.Tracing;
using Serilog;
using Serilog.Configuration;

namespace TradeImportsQuantityMgmt.Utils.Logging
{
    public static class ClientInfoLoggerConfigurationExtensions
    {
        public static LoggerConfiguration WithTraceId(
            this LoggerEnrichmentConfiguration enrichmentConfiguration,
            string headerName = "x-correlation-id",
            bool addValueIfHeaderAbsence = true
        )
        {
            ArgumentNullException.ThrowIfNull(enrichmentConfiguration);

            return enrichmentConfiguration.With(
                new TraceIdEnricher(headerName, addValueIfHeaderAbsence, new TraceContextAccessor())
            );
        }
    }
}
