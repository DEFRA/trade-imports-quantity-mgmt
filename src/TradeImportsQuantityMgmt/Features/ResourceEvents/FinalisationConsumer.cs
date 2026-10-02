using System.Text.Json;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Traces;
using Infrastructure.Messaging.Consuming;
using Microsoft.AspNetCore.Mvc;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Features.QuantityManagement;
using TradeImportsQuantityMgmt.Mappings;
using TradeImportsQuantityMgmt.Utils;

namespace TradeImportsQuantityMgmt.Features.ResourceEvents;

public class FinalisationConsumer(
    ILogger<FinalisationConsumer> logger,
    ITracesGatewayChedClient tracesGatewayChedClient,
    ITradeImportsDataApiClient tradeImportsDataApiClient,
    QuantityManagementOutcomeRecorder outcomeRecorder
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

        var response = await tradeImportsDataApiClient.GetTracesChedsByMrn(mrn, cancellationToken);
        var chedReferences = response.Cheds.Select(x => x.Ched.ExchangedDocument.Identifier).ToArray();

        if (!chedReferences.Any())
        {
            logger.LogInformation("No Traces CHEDS exist for MRN {MovementReferenceNumber}", mrn);

            return;
        }

        var finalisation = message.Resource.Finalisation;

        switch (finalisation)
        {
            case {
                FinalState: FinalState.Cleared
                    or FinalState.Destroyed
                    or FinalState.Seized
                    or FinalState.ReleasedToKingsWarehouse,
                IsManualRelease: false,
            }:
                await ProcessClearanceAsync(chedReferences, mrn, cancellationToken);
                break;

            case { FinalState: FinalState.CancelledAfterArrival or FinalState.CancelledWhilePreLodged }:
                await ProcessCancellationAsync(chedReferences, mrn, cancellationToken);
                break;

            default:
                logger.LogInformation("No action required for final state {FinalState}", finalisation?.FinalState);
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

        using var response = await tracesGatewayChedClient.ReleaseChedReservation(
            chedReference,
            mrn,
            cancellationToken
        );
        var outcome = await RecordOutcomeAsync(
            QuantityManagementOperation.ReleaseReservation,
            chedReference,
            mrn,
            response,
            cancellationToken
        );

        if (!outcome.IsSuccess)
            return;

        await SyncReservationWithDataApiAsync(chedReference, mrn, cancellationToken);
    }

    private async Task SyncReservationWithDataApiAsync(
        string chedReference,
        string mrn,
        CancellationToken cancellationToken
    )
    {
        var tracesReservation = await tracesGatewayChedClient.GetChedQuantities(chedReference, cancellationToken);
        var allocations = tracesReservation.Content?.Allocations;

        // No allocations for this CHED at all, or none left for this declaration once the
        // whole-CHED ledger is narrowed down to this MRN: nothing to reserve, so the record
        // (if any) should be removed rather than persisted as an empty "Unreserved" write.
        var reservation = allocations?.ToReservationForDataApi(chedReference, mrn);

        if (reservation is null || reservation.Status == ReservationStatus.Unreserved)
        {
            await tradeImportsDataApiClient.DeleteChedReservation(chedReference, mrn, cancellationToken);
            return;
        }

        var etag = await tradeImportsDataApiClient.GetChedReservationETag(chedReference, mrn, cancellationToken);
        await tradeImportsDataApiClient.PutChedReservation(chedReference, mrn, reservation, etag, cancellationToken);
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
        using var response = await tracesGatewayChedClient.DeleteChedReservation(chedReference, mrn, cancellationToken);
        var outcome = await RecordOutcomeAsync(
            QuantityManagementOperation.DeleteReservation,
            chedReference,
            mrn,
            response,
            cancellationToken
        );

        if (!outcome.IsSuccess)
            return;

        await tradeImportsDataApiClient.DeleteChedReservation(chedReference, mrn, cancellationToken);
    }

    private async Task<QuantityManagementOutcome> RecordOutcomeAsync(
        QuantityManagementOperation operation,
        string chedReference,
        string mrn,
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        // Only an unsuccessful response carries problem details worth reading.
        var problem = response.IsSuccessStatusCode ? null : await ReadProblemAsync(response, cancellationToken);

        var outcome = QuantityManagementOutcome.FromResponse(
            operation,
            chedReference,
            mrn,
            response.StatusCode,
            problem
        );
        outcomeRecorder.Record(outcome);

        return outcome;
    }

    private static async Task<ProblemDetails?> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (JsonException)
        {
            // An empty or non-JSON body (e.g. from a proxy) - there are no problem details to read.
            return null;
        }
    }
}
