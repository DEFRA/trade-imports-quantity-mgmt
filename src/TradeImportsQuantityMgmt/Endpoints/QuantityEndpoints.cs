using Defra.TradeImportsDataApi.Api.Client;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Filters;
using TradeImportsQuantityMgmt.Mappings;
using TradeImportsQuantityMgmt.Utils;

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

        if (response.IsSuccessful)
        {
            // Send the reservation record to the data-api, threading through the ETag of any
            // existing record so the write is an optimistic-concurrency update rather than a
            // blind overwrite.
            var etag = await tradeImportsDataApiClient.GetChedReservationETag(chedId, mrn, cancellationToken);

            var reservation = response.Content.ToReservationForDataApi(chedId, mrn);
            await tradeImportsDataApiClient.PutChedReservation(chedId, mrn, reservation, etag, cancellationToken);

            return Results.Json(
                response.Content?.ToApiContract(),
                contentType: MediaTypeAttribute.For<ChedDeclarationReservation>()
            );
        }

        return Results.Problem(
            statusCode: response.StatusCode != null ? (int)response.StatusCode : 500,
            detail: response.Error.Message
        );
    }
}
