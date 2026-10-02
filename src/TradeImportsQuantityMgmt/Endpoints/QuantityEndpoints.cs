using System.Net;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Traces;
using Refit;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Contract.Customs;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Features.QuantityManagement;
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
        QuantityManagementOutcomeRecorder outcomeRecorder,
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
        QuantityManagementOutcome? outcome = null;
        if (response.IsSuccessful)
        {
            outcomeRecorder.Record(
                QuantityManagementOutcome.FromResponse(
                    QuantityManagementOperation.PutReservation,
                    chedId,
                    mrn,
                    response.StatusCode ?? HttpStatusCode.OK,
                    null
                )
            );

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
            outcome = QuantityManagementOutcome.FromResponse(
                QuantityManagementOperation.PutReservation,
                chedId,
                mrn,
                apiException.StatusCode,
                apiException.Content
            );
            outcomeRecorder.Record(outcome);

            reservation = new Reservation()
            {
                ChedId = chedId,
                Mrn = mrn,
                Status = ReservationStatus.Unsuccessful,
                Timestamp = DateTime.UtcNow,
                UnsuccessfulReason = outcome.Reason,
            };
            await tradeImportsDataApiClient.PutChedReservation(chedId, mrn, reservation, etag, cancellationToken);
        }

        // ChedReservationProblemDetails.Reason never makes it into Extensions during deserialisation
        // (see QuantityManagementOutcome), so put the reason read from the raw body back in.
        Dictionary<string, object?>? extensions = null;
        if (problem is not null)
        {
            extensions = problem.Extensions?.ToDictionary(x => x.Key, object? (x) => x.Value) ?? [];
            if (outcome?.Reason is { } reason)
                extensions["reason"] = reason;
        }

        return Results.Problem(
            statusCode: response.StatusCode != null ? (int)response.StatusCode : 500,
            detail: response.Error.Message,
            extensions: extensions
        );
    }
}
