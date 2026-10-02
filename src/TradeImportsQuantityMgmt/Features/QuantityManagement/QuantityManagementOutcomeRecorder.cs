namespace TradeImportsQuantityMgmt.Features.QuantityManagement;

/// <summary>
/// Logs and emits metrics for every outcome response received from TRACES Quantity Management.
/// The log carries the CHED, MRN, operation, status code, reason and detail so an individual
/// response can be investigated; the trace id is added by the logging enrichers.
/// </summary>
public class QuantityManagementOutcomeRecorder(
    ILogger<QuantityManagementOutcomeRecorder> logger,
    QuantityManagementMetrics metrics
)
{
    public void Record(QuantityManagementOutcome outcome)
    {
        if (outcome.IsSuccess)
        {
            logger.LogInformation(
                "Quantity Management {Operation} for CHED {Ched} - MRN {MovementReferenceNumber} returned outcome {Outcome} with response code {ResponseCode}",
                outcome.Operation,
                outcome.ChedId,
                outcome.Mrn,
                outcome.Outcome,
                (int)outcome.StatusCode
            );
        }
        else
        {
            logger.LogWarning(
                "Quantity Management {Operation} for CHED {Ched} - MRN {MovementReferenceNumber} returned outcome {Outcome} with response code {ResponseCode}, reason {Reason} and detail {Detail}",
                outcome.Operation,
                outcome.ChedId,
                outcome.Mrn,
                outcome.Outcome,
                (int)outcome.StatusCode,
                outcome.Reason,
                outcome.Detail
            );
        }

        metrics.Outcome(outcome);
    }
}
