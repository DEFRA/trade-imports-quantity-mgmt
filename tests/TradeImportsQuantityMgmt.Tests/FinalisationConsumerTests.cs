using System.Net;
using Amazon.SQS.Model;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure;
using Infrastructure.Messaging.Consuming;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Features.ResourceEvents;

namespace TradeImportsQuantityMgmt.Tests;

public class FinalisationConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_LogsWarning_WhenReleaseFails()
    {
        // Arrange
        var mrn = "25GBVLKTCO0HN7MUA4";
        var ched = "CHEDA.GB.2026.1234567";

        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .ReleaseChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient);

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

        var message = new Message { Body = resourceEvent.ToJson(), MessageId = "1" };

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

        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .DeleteChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient);

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

        var message = new Message { Body = resourceEvent.ToJson(), MessageId = "1" };

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
    public async Task ConsumeAsync_LogsWarning_WhenResourceIsNull()
    {
        // Arrange
        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        var logger = Substitute.For<ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient);

        var resourceEvent = new ResourceEvent<CustomsDeclarationEvent>
        {
            Resource = null,
            ResourceId = "resourceId",
            Operation = "operation",
            ResourceType = nameof(CustomsDeclarationEvent),
        };

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

        var context = new MessageContext
        {
            Message = message,
            QueueUrl = "queue",
            ConsumerType = typeof(FinalisationConsumer),
        };

        // Act
        await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken);

        // Assert: a warning should be logged indicating deserialisation
        var expectedMessage = "Message for trace {11111111-1111-1111-1111-111111111111} could not be deserialised";

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
