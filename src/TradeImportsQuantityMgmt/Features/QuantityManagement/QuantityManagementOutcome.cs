using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TradeImportsQuantityMgmt.Features.QuantityManagement;

/// <summary>
/// The TRACES Quantity Management operation that produced an outcome response, named after the
/// Trade Gateway call that was made.
/// </summary>
public enum QuantityManagementOperation
{
    PutReservation,
    ReleaseReservation,
    DeleteReservation,
}

/// <summary>
/// An outcome response received from TRACES Quantity Management. An unsuccessful response carries
/// the TRACES <c>reason</c> and <c>detail</c> from its problem details, when supplied.
/// </summary>
public sealed record QuantityManagementOutcome(
    QuantityManagementOperation Operation,
    string ChedId,
    string Mrn,
    HttpStatusCode StatusCode,
    string? Reason,
    string? Detail
)
{
    public const string SuccessOutcome = "Success";
    public const string UnsuccessfulOutcome = "Unsuccessful";

    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;

    public string Outcome => IsSuccess ? SuccessOutcome : UnsuccessfulOutcome;

    // Problem details are deliberately read as the framework ProblemDetails rather than the gateway's
    // ChedReservationProblemDetails: the latter's get-only Reason property claims the "reason" key
    // during deserialisation, so the value never reaches Extensions and is lost.
    public static QuantityManagementOutcome FromResponse(
        QuantityManagementOperation operation,
        string chedId,
        string mrn,
        HttpStatusCode statusCode,
        ProblemDetails? problem
    )
    {
        object? reasonValue = null;
        problem?.Extensions.TryGetValue("reason", out reasonValue);
        var reason = reasonValue switch
        {
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            string value => value,
            _ => null,
        };

        return new QuantityManagementOutcome(operation, chedId, mrn, statusCode, reason, problem?.Detail);
    }
}
