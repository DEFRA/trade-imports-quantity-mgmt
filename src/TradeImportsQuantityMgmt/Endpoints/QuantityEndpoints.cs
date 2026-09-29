using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Traces;
using Refit;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Contract.Customs;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Filters;
using TradeImportsQuantityMgmt.Mappings;
using TradeImportsQuantityMgmt.Utils;
using ChedDeclarationReservation = TradeImportsQuantityMgmt.Contract.ChedDeclarationReservation;
using ChedReservationRequest = TradeImportsQuantityMgmt.Contract.ChedReservationRequest;

namespace TradeImportsQuantityMgmt.Endpoints;

/// <summary>
/// Customs quantity management, under its own <c>customs/</c> prefix rather than beneath
/// <c>certificates/</c> so that the existing <c>ched-reader</c> grant on
/// <c>/certificates/cheds/**</c> cannot silently confer access to customs quantity data.
/// The two halves of the same upstream operation: the ledger read sends
/// <c>QuantityManagementIndication = "0"</c>, the reservation sends <c>"1"</c> and mutates
/// state upstream.
/// </summary>
public static class QuantityEndpoints
{
    public static void UseChedQuantityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("customs");

        group
            .MapPut("cheds/{chedId}/declarations/{mrn}/reservation", PutReservation)
            .Validates<ChedReservationRequest>()
            .Produces<ChedDeclarationReservation>(200, MediaTypeAttribute.For<ChedDeclarationReservation>())
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> PutReservation(
        string chedId,
        string mrn,
        ChedReservationRequest request,
        ITracesGatewayChedClient tracesGatewayChedClient,
        ITradeImportsDataApiClient tradeImportsDataApiClient,
        CancellationToken cancellationToken
    )
    {
        var gatewayRequest = request.ToTradeGatewayDto();
        var response = await tracesGatewayChedClient.PutChedReservation(chedId, mrn, gatewayRequest, cancellationToken);

        // Send the reservation record to the data-api, threading through the ETag of any
        // existing record so the write is an optimistic-concurrency update rather than a
        // blind overwrite.
        var etag = await tradeImportsDataApiClient.GetChedReservationETag(chedId, mrn, cancellationToken);
        Reservation reservation;
        ChedReservationProblemDetails? problem = null;
        string? problemContent = null;
        if (response.IsSuccessful)
        {
            reservation = response.Content.ToReservationForDataApi(chedId, mrn);
            await tradeImportsDataApiClient.PutChedReservation(chedId, mrn, reservation, etag, cancellationToken);

            return Results.Json(
                response.Content?.ToApiContract(),
                contentType: MediaTypeAttribute.For<ChedDeclarationReservation>()
            );
        }
        else if (response.Error is ApiException apiException)
        {
            problem = await apiException.GetContentAsAsync<ChedReservationProblemDetails>();
            problemContent = apiException.Content;
            TryGetReason(problemContent, out var unsuccessfulReason);

            reservation = new Reservation()
            {
                ChedId = chedId,
                Mrn = mrn,
                Status = ReservationStatus.Unsuccessful,
                Timestamp = DateTime.UtcNow,
                UnsuccessfulReason = unsuccessfulReason,
            };
            await tradeImportsDataApiClient.PutChedReservation(chedId, mrn, reservation, etag, cancellationToken);
        }

        // ChedReservationProblemDetails.Reason is a get-only property computed from Extensions["reason"],
        // but that means System.Text.Json already claims the "reason" key for the (unwritable) Reason
        // property during deserialisation, so it never actually reaches Extensions and Reason is always
        // null. Read "reason" back out of the raw body ourselves so it isn't silently lost.
        Dictionary<string, object?>? extensions = null;
        if (problem is not null)
        {
            extensions = problem.Extensions?.ToDictionary(x => x.Key, object? (x) => x.Value) ?? [];
            if (TryGetReason(problemContent, out var reason))
                extensions["reason"] = reason;
        }

        return Results.Problem(
            statusCode: response.StatusCode != null ? (int)response.StatusCode : 500,
            detail: response.Error.Message,
            extensions: extensions
        );
    }

    private static bool TryGetReason(string? content, out string? reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(content))
            return false;

        try
        {
            using var document = JsonDocument.Parse(content);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (
                    string.Equals(property.Name, "reason", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                )
                {
                    reason = property.Value.GetString();
                    return reason is not null;
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON, or not an object - nothing to extract.
        }

        return false;
    }
}
