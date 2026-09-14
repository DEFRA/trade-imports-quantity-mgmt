using System.Net;
using System.Net.Http;
using Amazon.SQS.Model;
using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Infrastructure;
using Infrastructure.Messaging.Consuming;
using NSubstitute;
using Trade.Gateway.Api.Client.Clients;
using TradeImportsQuantityMgmt.Exceptions;
using TradeImportsQuantityMgmt.Features.ResourceEvents;
using ResourceEvents = TradeImportsQuantityMgmt.Features.ResourceEvents;

namespace TradeImportsQuantityMgmt.Tests;

public class FinalisationConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_ThrowsQuantityReleaseFailureException_WhenReleaseFails()
    {
        // Arrange
        var mrn = "25GBVLKTCO0HN7MUA4";
        var ched = "CHEDA.GB.2026.1234567";

        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .ReleaseChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient);

        var customsEvent = new CustomsDeclarationEvent
        {
            Id = mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = ResourceEvents.FinalState.Cleared,
                IsManualRelease = false,
            },
            ClearanceRequest = new ClearanceRequest
            {
                Commodities = new[]
                {
                    new Commodity
                    {
                        Documents = new[]
                        {
                            new ImportDocument
                            {
                                DocumentCode = "9115",
                                DocumentReference = new ImportDocumentReference(ched),
                            },
                        },
                    },
                },
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<QuantityReleaseFailureException>(async () =>
            await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken)
        );

        ex.Mrn.Should().Be(mrn);
        ex.Ched.Should().Be(ched);
    }

    [Fact]
    public async Task ConsumeAsync_DoesNotThrow_WhenReleaseSucceeds()
    {
        // Arrange
        var mrn = "25GBVLKTCO0HN7MUA4";
        var ched = "CHEDA.GB.2026.1234567";

        var tracesClient = Substitute.For<ITracesGatewayChedClient>();
        tracesClient
            .ReleaseChedReservation(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<FinalisationConsumer>>();

        var consumer = new FinalisationConsumer(logger, tracesClient);

        var customsEvent = new CustomsDeclarationEvent
        {
            Id = mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = ResourceEvents.FinalState.Cleared,
                IsManualRelease = false,
            },
            ClearanceRequest = new ClearanceRequest
            {
                Commodities = new[]
                {
                    new Commodity
                    {
                        Documents = new[]
                        {
                            new ImportDocument
                            {
                                DocumentCode = "9115",
                                DocumentReference = new ImportDocumentReference(ched),
                            },
                        },
                    },
                },
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
        var ex = await Record.ExceptionAsync(async () =>
            await consumer.ConsumeAsync(context, TestContext.Current.CancellationToken)
        );

        // Assert
        ex.Should().BeNull();
        await tracesClient.Received(1).ReleaseChedReservation(ched, mrn, TestContext.Current.CancellationToken);
    }
}
