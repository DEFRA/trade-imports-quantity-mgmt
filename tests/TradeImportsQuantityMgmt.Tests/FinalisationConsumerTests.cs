using System.Net;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Api.Client;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Traces;
using Infrastructure;
using Infrastructure.Messaging.Consuming;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;
using Trade.Gateway.Api.Client.Clients;
using Trade.Gateway.Api.Contract.Customs;
using TradeImportsQuantityMgmt.Features.ResourceEvents;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests;

public class FinalisationConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_LogsWarning_WhenReleaseFails()
    {
        // Arrange
        var mrn = "25GBVLKTCO0HN7MUA4";
        var ched = "CHEDA.GB.2026.1234567";

        var dataApiClient = Substitute.For<ITradeImportsDataApiClient>();
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .GetChedQuantities(ched, TestContext.Current.CancellationToken)
            .Returns(
                Task.FromResult(
                    new ApiResponse<ChedQuantityLedger>(
                        new HttpResponseMessage(HttpStatusCode.InternalServerError),
                        null!,
                        new RefitSettings()
                    )
                )
            );

        tracesClient
            .ReleaseChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient, dataApiClient);

        var customsEvent = new CustomsDeclarationEvent
        {
            Id = mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = FinalState.Cleared,
                IsManualRelease = false,
            },
            ClearanceRequest = new ClearanceRequest
            {
                Commodities =
                [
                    new Commodity
                    {
                        Documents =
                        [
                            new ImportDocument
                            {
                                DocumentCode = "9115",
                                DocumentReference = new ImportDocumentReference(ched),
                            },
                        ],
                    },
                ],
            },
        };

        var resourceEvent = new ResourceEvent<CustomsDeclarationEvent>
        {
            Resource = customsEvent,
            ResourceId = "resourceId",
            Operation = "operation",
            ResourceType = nameof(CustomsDeclarationEvent),
        };

        var message = new Message
        {
            Body = resourceEvent.ToJson(),
            MessageId = "1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            {
                {
                    "ResourceId",
                    new MessageAttributeValue() { StringValue = mrn }
                },
            },
        };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "queue",
            ConsumerType = typeof(FinalisationConsumer),
        };

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Verify release was attempted and a warning was logged
        await tracesClient.Received(1).ReleaseChedReservation(ched, mrn, TestContext.Current.CancellationToken);

        var expectedMessage =
            $"Releasing goods for MRN {mrn} - CHED {ched} returned response code {HttpStatusCode.InternalServerError}";

        logger
            .Received(1)
            .Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                Arg.Any<Microsoft.Extensions.Logging.EventId>(),
                Arg.Is<object>(o => (o != null ? o.ToString() : string.Empty) == expectedMessage),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ConsumeAsync_LogsWarning_WhenDeleteFails()
    {
        // Arrange
        var mrn = "25GBVLKTCO0HN7MUA4";
        var ched = "CHEDA.GB.2026.9876543";

        var dataApiClient = Substitute.For<ITradeImportsDataApiClient>();
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .DeleteChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient, dataApiClient);

        var customsEvent = new CustomsDeclarationEvent
        {
            Id = mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = FinalState.CancelledAfterArrival,
                IsManualRelease = false,
            },
            ClearanceRequest = new ClearanceRequest
            {
                Commodities =
                [
                    new Commodity
                    {
                        Documents =
                        [
                            new ImportDocument
                            {
                                DocumentCode = "9115",
                                DocumentReference = new ImportDocumentReference(ched),
                            },
                        ],
                    },
                ],
            },
        };

        var resourceEvent = new ResourceEvent<CustomsDeclarationEvent>
        {
            Resource = customsEvent,
            ResourceId = "resourceId",
            Operation = "operation",
            ResourceType = nameof(CustomsDeclarationEvent),
        };

        var message = new Message
        {
            Body = resourceEvent.ToJson(),
            MessageId = "1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            {
                {
                    "ResourceId",
                    new MessageAttributeValue() { StringValue = mrn }
                },
            },
        };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "queue",
            ConsumerType = typeof(FinalisationConsumer),
        };

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Assert
        await tracesClient.Received(1).DeleteChedReservation(ched, mrn, TestContext.Current.CancellationToken);

        var expectedMessage =
            $"Deleting reservation for MRN {mrn} - CHED {ched} returned response code {HttpStatusCode.InternalServerError}";

        logger
            .Received(1)
            .Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                Arg.Any<Microsoft.Extensions.Logging.EventId>(),
                Arg.Is<object>(o => (o != null ? o.ToString() : string.Empty) == expectedMessage),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    [Fact]
    public async Task ConsumeAsync_Cleared_WritesOnlyThisDeclarationsAllocationToDataApi()
    {
        // Arrange
        var mrn = "26GBTHISDECL";
        var otherMrn = "99GBOTHERDECL";
        var ched = "CHEDA.GB.2026.1234567";

        var dataApiClient = Substitute.For<ITradeImportsDataApiClient>();
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();

        tracesClient
            .ReleaseChedReservation(ched, mrn, TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // The ledger is CHED-wide: another declaration on the same CHED still holds a reservation,
        // while this declaration's own allocation has been consumed. The consumer must not
        // attribute the other declaration's reservation to this one.
        var ledger = new ChedQuantityLedger
        {
            Available = [],
            Allocations = new QuantityAllocations
            {
                Reserved =
                [
                    new AllocatedCommodityQuantity
                    {
                        GoodsItemNumber = 1,
                        UnitOfMeasure = "ASVX",
                        Quantity = 111m,
                        DeclarationReference = new DeclarationReference
                        {
                            Type = DeclarationReferenceType.Mrn,
                            Value = otherMrn,
                        },
                    },
                ],
                Consumed =
                [
                    new AllocatedCommodityQuantity
                    {
                        GoodsItemNumber = 2,
                        UnitOfMeasure = "KGM",
                        Quantity = 300m,
                        DeclarationReference = new DeclarationReference
                        {
                            Type = DeclarationReferenceType.Mrn,
                            Value = mrn,
                        },
                    },
                ],
            },
        };

        tracesClient
            .GetChedQuantities(ched, TestContext.Current.CancellationToken)
            .Returns(
                Task.FromResult(
                    new ApiResponse<ChedQuantityLedger>(
                        new HttpResponseMessage(HttpStatusCode.OK),
                        ledger,
                        new RefitSettings()
                    )
                )
            );

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();
        var consumer = new FinalisationConsumer(logger, tracesClient, dataApiClient);

        var context = BuildClearedMessageContext(mrn, ched);

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Assert: only this declaration's consumed allocation was persisted, at "Consumed" status.
        await dataApiClient
            .Received(1)
            .PutChedReservation(
                ched,
                mrn,
                Arg.Is<Reservation>(r =>
                    r.Status == ReservationStatus.Consumed
                    && r.Commodities.Length == 1
                    && r.Commodities[0].Quantity == 300m
                ),
                null,
                TestContext.Current.CancellationToken
            );
    }

    [Fact]
    public async Task ConsumeAsync_Cleared_DeletesRatherThanPersistingAnEmptyRecord_WhenNoAllocationsRemainForThisDeclaration()
    {
        // Arrange
        var mrn = "26GBTHISDECL";
        var otherMrn = "99GBOTHERDECL";
        var ched = "CHEDA.GB.2026.1234567";

        var dataApiClient = Substitute.For<ITradeImportsDataApiClient>();
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();

        tracesClient
            .ReleaseChedReservation(ched, mrn, TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Only another declaration's rows exist on the CHED - nothing for this MRN once filtered.
        var ledger = new ChedQuantityLedger
        {
            Available = [],
            Allocations = new QuantityAllocations
            {
                Reserved =
                [
                    new AllocatedCommodityQuantity
                    {
                        GoodsItemNumber = 1,
                        UnitOfMeasure = "ASVX",
                        Quantity = 111m,
                        DeclarationReference = new DeclarationReference
                        {
                            Type = DeclarationReferenceType.Mrn,
                            Value = otherMrn,
                        },
                    },
                ],
                Consumed = [],
            },
        };

        tracesClient
            .GetChedQuantities(ched, TestContext.Current.CancellationToken)
            .Returns(
                Task.FromResult(
                    new ApiResponse<ChedQuantityLedger>(
                        new HttpResponseMessage(HttpStatusCode.OK),
                        ledger,
                        new RefitSettings()
                    )
                )
            );

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();
        var consumer = new FinalisationConsumer(logger, tracesClient, dataApiClient);

        var context = BuildClearedMessageContext(mrn, ched);

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Assert: no misleading "Unreserved" record is written; any existing record is removed instead.
        await dataApiClient.Received(1).DeleteChedReservation(ched, mrn, TestContext.Current.CancellationToken);
        await dataApiClient
            .DidNotReceive()
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Reservation>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static MessageContext BuildClearedMessageContext(string mrn, string ched)
    {
        var customsEvent = new CustomsDeclarationEvent
        {
            Id = mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = FinalState.Cleared,
                IsManualRelease = false,
            },
            ClearanceRequest = new ClearanceRequest
            {
                Commodities =
                [
                    new Commodity
                    {
                        Documents =
                        [
                            new ImportDocument
                            {
                                DocumentCode = "9115",
                                DocumentReference = new ImportDocumentReference(ched),
                            },
                        ],
                    },
                ],
            },
        };

        var resourceEvent = new ResourceEvent<CustomsDeclarationEvent>
        {
            Resource = customsEvent,
            ResourceId = "resourceId",
            Operation = "operation",
            ResourceType = nameof(CustomsDeclarationEvent),
        };

        var message = new Message
        {
            Body = resourceEvent.ToJson(),
            MessageId = "1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>()
            {
                {
                    "ResourceId",
                    new MessageAttributeValue() { StringValue = mrn }
                },
            },
        };

        return new MessageContext
        {
            Message = message,
            QueueUrl = "queue",
            ConsumerType = typeof(FinalisationConsumer),
        };
    }

    [Fact]
    public async Task ConsumeAsync_LogsWarning_WhenResourceIsNull()
    {
        // Arrange
        var dataApiClient = Substitute.For<ITradeImportsDataApiClient>();
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient, dataApiClient);

        var resourceEvent = new ResourceEvent<CustomsDeclarationEvent>
        {
            Resource = null,
            ResourceId = "resourceId",
            Operation = "operation",
            ResourceType = nameof(CustomsDeclarationEvent),
        };

        var mrn = "25GBVLKTCO0HN7MUA4";

        var message = new Message
        {
            Body = resourceEvent.ToJson(),
            MessageId = "1",
            MessageAttributes = [],
        };

        message.MessageAttributes[MetricNames.TraceKey] = new MessageAttributeValue
        {
            DataType = "String",
            StringValue = "{11111111-1111-1111-1111-111111111111}",
        };

        message.MessageAttributes["ResourceId"] = new MessageAttributeValue { DataType = "String", StringValue = mrn };

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "queue",
            ConsumerType = typeof(FinalisationConsumer),
        };

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Assert: a warning should be logged indicating deserialisation
        var expectedMessage = $"Message for MRN {mrn} could not be deserialised";

        logger
            .Received(1)
            .Log(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                Arg.Any<Microsoft.Extensions.Logging.EventId>(),
                Arg.Is<object>(o => (o != null ? o.ToString() : string.Empty) == expectedMessage),
                Arg.Any<Exception>(),
                Arg.Any<Func<object, Exception?, string>>()
            );
    }
}
