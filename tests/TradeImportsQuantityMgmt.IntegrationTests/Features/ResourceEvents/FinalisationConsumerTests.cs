using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

using AwesomeAssertions;

using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;

using NSubstitute;
using NSubstitute.ClearExtensions;

using System.Diagnostics;
using System.Text.Json;

using ResourceEventFinalState = TradeImportsQuantityMgmt.Features.ResourceEvents.FinalState;

namespace TradeImportsQuantityMgmt.IntegrationTests.Features.ResourceEvents;

[Collection(IntegrationTestCollection.Name)]
public class FinalisationConsumerTests(TradeGatewayWebApplicationFactory factory) : IAsyncLifetime
{
    // Use localhost for tests (Floci/localstack is expected to be reachable at localhost:4566 in test environment)
    private const string QueueUrl = "http://localhost:4566/000000000000/trade_imports_data_upserted_quantity_mgmt";

    private readonly AmazonSQSClient _sqsClient = new AmazonSQSClient(
        new BasicAWSCredentials("test", "test"),
        new AmazonSQSConfig { ServiceURL = "http://localhost:4566", AuthenticationRegion = "eu-west-2" }
    );

    internal const string Mrn = "25GBVLKTCO0HN7MUA4";
    internal const string Ched = "CHEDA.GB.2026.1234567";

    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    private HttpClient? _httpClient;

    [Fact]
    public async Task Message_With_Finalisation_SubType_Is_Processed()
    {
        // Start the app so the background SQS consumer runs
        _httpClient = factory.CreateClient();

        // Ensure the traces gateway client returns a successful response for the release call
        factory
            .TracesGatewayChedClient.ReleaseChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)));

        var customsDeclarationEvent = new CustomsDeclarationEvent
        {
            Id = Mrn,
            Finalisation = new Finalisation
            {
                ExternalVersion = 1,
                FinalState = ResourceEventFinalState.Cleared,
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
                                DocumentReference = new ImportDocumentReference(Ched),
                                DocumentCode = "9115",
                            },
                        ],
                    },
                ],
            },
        };

        var messageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            [nameof(ResourceEvent<object>.SubResourceType)] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = nameof(Finalisation),
            },
        };

        await _sqsClient.SendMessageAsync(
            new SendMessageRequest
            {
                QueueUrl = QueueUrl,
                MessageBody = JsonSerializer.Serialize(
                    new ResourceEvent<CustomsDeclarationEvent>
                    {
                        Resource = customsDeclarationEvent,
                        ResourceId = "resourceId",
                        Operation = "operation",
                        ResourceType = nameof(CustomsDeclarationEvent),
                    }
                ),
                MessageAttributes = messageAttributes,
            },
            _cancellationToken
        );

        var releaseProcessed = await WaitHelper.WaitUntilAsync(
            async () =>
            {
                try
                {
                    await factory
                        .TracesGatewayChedClient.Received(1)
                        .ReleaseChedReservation(Ched, Mrn, Arg.Any<CancellationToken>());
                    return true;
                }
                catch
                {
                    return false;
                }
            },
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(500),
            _cancellationToken
        );

        releaseProcessed.Should().BeTrue("ReleaseChedReservation should be called within 120s");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
    }

    public async ValueTask InitializeAsync()
    {
        await WireMockStubber.ResetAsync(factory.WireMockBaseUrl);
        await WireMockStubber.StubChedReleaseAsync(factory.WireMockBaseUrl, Mrn, Ched, CancellationToken.None);

        await _sqsClient.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = QueueUrl }, _cancellationToken);

        // The traces gateway client is a singleton shared across every test in the collection -
        // clear it down so a previous test's setup can't leak into this one.
        factory.TracesGatewayChedClient.ClearSubstitute();

        await Task.Delay(100, _cancellationToken);
    }
}
