using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure.Messaging.Consuming;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Features.ResourceEvents;

public class FinalisationConsumer(
    ILogger<FinalisationConsumer> logger,
    ITracesGatewayChedClient tracesGatewayChedClient,
    ITradeImportsDataApiClient tradeImportsDataApiClient
) : IMessageConsumer
{
    public async Task ConsumeAsync(MessageContext context, CancellationToken cancellationToken = default)
    {
        var mrn = context.GetResourceId();
        logger.LogInformation("Processing Resource Event for MRN: {Mrn}", mrn);

        var message = context.ParseMessage<ResourceEvent<CustomsDeclarationEvent>>();

        if (message?.Resource == null)
        {
            logger.LogWarning("Message for MRN {Mrn} could not be deserialised", mrn);
            return;
        }

        var chedReferences = message.Resource.ClearanceRequest.GetTracesCheds().ToArray();

        if (!chedReferences.Any())
        {
            logger.LogInformation("No Traces CHEDS exist for MRN {MovementReferenceNumber}", mrn);

            return;
        }

        switch (message.Resource.Finalisation?.FinalState)
        {
            case FinalState.Cleared when message.Resource.Finalisation?.IsManualRelease is false:
                await ProcessClearanceAsync(chedReferences, mrn, cancellationToken);
                break;

            case FinalState.CancelledAfterArrival:
            case FinalState.CancelledWhilePreLodged:
                await ProcessCancellationAsync(chedReferences, mrn, cancellationToken);
                break;

            default:
                logger.LogInformation(
                    "No action required for final state {FinalState}",
                    message.Resource.Finalisation?.FinalState
                );
                break;
        }
    }

    private async Task ProcessClearanceAsync(
        IEnumerable<string> chedReferences,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        foreach (var chedReference in chedReferences)
        {
            await ReleaseChedReservationAsync(chedReference, mrn, cancellationToken);
        }
    }

    private async Task ReleaseChedReservationAsync(
        string chedReference,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Releasing goods for MRN {MovementReferenceNumber} - CHED {Ched}", mrn, chedReference);

        var response = await tracesGatewayChedClient.ReleaseChedReservation(chedReference, mrn, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Releasing goods for MRN {MovementReferenceNumber} - CHED {Ched} returned response code {ResponseCode}",
                mrn,
                chedReference,
                response.StatusCode
            );
            return;
        }

        await SyncReservationWithDataApiAsync(chedReference, mrn, cancellationToken);
    }

    private async Task SyncReservationWithDataApiAsync(
        string chedReference,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        var tracesReservation = await tracesGatewayChedClient.GetChedQuantities(chedReference, cancellationToken);
        var etag = await GetEtag(chedReference, mrn, tradeImportsDataApiClient, cancellationToken);

        if (tracesReservation.Content?.Allocations != null)
        {
            var reservation = tracesReservation.Content.Allocations.ToReservationForDataApi(chedReference, mrn);
            await tradeImportsDataApiClient.PutChedReservation(
                chedReference,
                mrn,
                reservation,
                etag,
                cancellationToken
            );
        }
        else
        {
            await tradeImportsDataApiClient.DeleteChedReservation(chedReference, mrn, null!, cancellationToken);
        }
    }

    private async Task ProcessCancellationAsync(
        IEnumerable<string> chedReferences,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Deleting reservation for MRN {Mrn}", mrn);

        foreach (var chedReference in chedReferences)
        {
            await DeleteReservationAsync(chedReference, mrn, cancellationToken);
        }
    }

    private async Task DeleteReservationAsync(string chedReference, string mrn, CancellationToken cancellationToken)
    {
        var response = await tracesGatewayChedClient.DeleteChedReservation(chedReference, mrn, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Deleting reservation for MRN {MovementReferenceNumber} - CHED {Ched} returned response code {ResponseCode}",
                mrn,
                chedReference,
                response.StatusCode
            );
            return;
        }

        await tradeImportsDataApiClient.DeleteChedReservation(chedReference, mrn, null!, cancellationToken);
    }

    private static async Task<string?> GetEtag(
        string chedId,
        string mrn,
        ITradeImportsDataApiClient tradeImportsDataApiClient,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingReservation = await tradeImportsDataApiClient.GetChedReservation(
                chedId,
                mrn,
                cancellationToken
            );
            return existingReservation?.ETag;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
