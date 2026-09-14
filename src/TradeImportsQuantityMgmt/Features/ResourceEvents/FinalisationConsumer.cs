using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure;
using Infrastructure.Messaging.Consuming;
using Microsoft.Net.Http.Headers;
using Trade.Gateway.Api.Client.Clients;

namespace TradeImportsQuantityMgmt.Features.ResourceEvents
{
    public class FinalisationConsumer(
        ILogger<FinalisationConsumer> logger,
        ITracesGatewayChedClient tracesGatewayChedClient
    ) : IMessageConsumer
    {
        public async Task ConsumeAsync(MessageContext context, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Processing Resource Event for trace {TraceId}", context.GetTraceId());

            var message = context.Body.FromJson<ResourceEvent<CustomsDeclarationEvent>>();
            var movementReferenceNumber = message!.Resource!.Id;

            var chedReferences = message.Resource!.ClearanceRequest!.GetTracesCheds().ToArray();

            if (!chedReferences.Any())
            {
                logger.LogInformation(
                    "No Traces CHEDS exist for MRN {MovementReferenceNumber}",
                    movementReferenceNumber
                );

                return;
            }

            switch (message.Resource.Finalisation?.FinalState)
            {
                case FinalState.Cleared when message.Resource.Finalisation?.IsManualRelease == false:

                    foreach (var chedReference in chedReferences)
                    {
                        logger.LogInformation(
                            "Reserving and releasing goods for MRN {MovementReferenceNumber} - CHED {Ched}",
                            movementReferenceNumber,
                            chedReference
                        );

                        var response = await tracesGatewayChedClient.ReleaseChedReservation(
                            chedReference,
                            movementReferenceNumber,
                            cancellationToken
                        );

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new QuantityReleaseFailureException(movementReferenceNumber, chedReference);
                        }
                    }
                    break;

                case FinalState.CancelledAfterArrival:
                case FinalState.CancelledWhilePreLodged:

                    logger.LogInformation("Clearing goods for MRN {MovementReferenceNumber}", movementReferenceNumber);
                    foreach (var chedReference in chedReferences)
                    {
                        var response = await tracesGatewayChedClient.DeleteChedReservation(
                            chedReference,
                            movementReferenceNumber,
                            cancellationToken
                        );

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new QuantityReleaseFailureException(movementReferenceNumber, chedReference);
                        }
                    }
                    break;

                default:
                    logger.LogInformation(
                        "No action required for final state {FinalState}",
                        message?.Resource?.Finalisation?.FinalState
                    );
                    break;
            }
        }
    }
}
