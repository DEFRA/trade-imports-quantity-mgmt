using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure.Messaging.Consuming;
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

            var message = context.ParseMessage<ResourceEvent<CustomsDeclarationEvent>>();

            if (message?.Resource == null)
            {
                logger.LogWarning("Message for trace {TraceId} could not be deserialised", context.GetTraceId());
                return;
            }

            var movementReferenceNumber = message.Resource.Id;

            var chedReferences = message.Resource.ClearanceRequest.GetTracesCheds().ToArray() ?? [];

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
                case FinalState.Cleared when message.Resource.Finalisation?.IsManualRelease is false:

                    foreach (var chedReference in chedReferences)
                    {
                        logger.LogInformation(
                            "Releasing goods for MRN {MovementReferenceNumber} - CHED {Ched}",
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
                            logger.LogWarning(
                                "Releasing goods for MRN {MovementReferenceNumber} - CHED {Ched} returned response code {ResponseCode}",
                                movementReferenceNumber,
                                chedReference,
                                response.StatusCode
                            );
                        }
                    }
                    break;

                case FinalState.CancelledAfterArrival:
                case FinalState.CancelledWhilePreLodged:

                    logger.LogInformation(
                        "Deleting reservation for MRN {MovementReferenceNumber}",
                        movementReferenceNumber
                    );
                    foreach (var chedReference in chedReferences)
                    {
                        var response = await tracesGatewayChedClient.DeleteChedReservation(
                            chedReference,
                            movementReferenceNumber,
                            cancellationToken
                        );

                        if (!response.IsSuccessStatusCode)
                        {
                            logger.LogWarning(
                                "Deleting reservation for MRN {MovementReferenceNumber} - CHED {Ched} returned response code {ResponseCode}",
                                movementReferenceNumber,
                                chedReference,
                                response.StatusCode
                            );
                        }
                    }
                    break;

                default:
                    logger.LogInformation(
                        "No action required for final state {FinalState}",
                        message.Resource.Finalisation?.FinalState
                    );
                    break;
            }
        }
    }
}
