using System.Net;
using System.Text.Json;

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
/// An outcome response received from TRACES Quantity Management. A successful response has the
/// outcome <see cref="SuccessOutcome"/>; an unsuccessful one carries the <c>reason</c> from the
/// problem details extensions, or <see cref="UnknownOutcome"/> when TRACES didn't supply one.
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
    public const string UnknownOutcome = "Unknown";

    public bool IsSuccess => (int)StatusCode is >= 200 and <= 299;

    public string Outcome => IsSuccess ? SuccessOutcome : Reason ?? UnknownOutcome;

    public static QuantityManagementOutcome FromResponse(
        QuantityManagementOperation operation,
        string chedId,
        string mrn,
        HttpStatusCode statusCode,
        string? content
    )
    {
        var outcome = new QuantityManagementOutcome(operation, chedId, mrn, statusCode, null, null);
        if (outcome.IsSuccess)
            return outcome;

        return outcome with
        {
            Reason = TryGetString(content, "reason"),
            Detail = TryGetString(content, "detail"),
        };
    }

    // ChedReservationProblemDetails.Reason is a get-only property computed from Extensions["reason"],
    // but that means System.Text.Json already claims the "reason" key for the (unwritable) Reason
    // property during deserialisation, so it never actually reaches Extensions and Reason is always
    // null. Read the problem details fields back out of the raw body ourselves so they aren't lost.
    private static string? TryGetString(string? content, string propertyName)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (
                    string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                )
                {
                    return property.Value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON - nothing to extract.
        }

        return null;
    }
}
