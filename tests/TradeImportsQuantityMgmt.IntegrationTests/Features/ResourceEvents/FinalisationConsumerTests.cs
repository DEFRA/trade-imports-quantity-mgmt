using System.Net;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;

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

        var customsDeclarationEvent = new CustomsDeclarationEvent
        {
            Id = Mrn,
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
            ["ResourceId"] = new MessageAttributeValue { DataType = "String", StringValue = Mrn },
        };

        var sendRequest = new SendMessageRequest
        {
            QueueUrl = QueueUrl,
            MessageBody = JsonSerializer.Serialize(
                new ResourceEvent<CustomsDeclarationEvent>
                {
                    Resource = customsDeclarationEvent,
                    ResourceId = "resourceId",
                    Operation = "operation",
                    ResourceType = nameof(CustomsDeclarationEvent),
                },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            ),
            MessageAttributes = messageAttributes,
        };

        var expectedPath = $"/customs/cheds/{Ched}/declarations/{Mrn}/reservation/release";

        // The local SQS emulator's long-poll occasionally misses a message sent just as a poll is
        // already in flight, silently stalling delivery for the rest of the wait window. Re-sending
        // (the consumer's handling is idempotent from this test's point of view - it only asserts
        // the release endpoint was hit at least once) hedges against that single missed poll cycle
        // without masking a genuine failure to process the message at all.
        var releaseProcessed = false;
        for (var attempt = 1; attempt <= 3 && !releaseProcessed; attempt++)
        {
            await _sqsClient.SendMessageAsync(sendRequest, _cancellationToken);

            releaseProcessed = await WaitHelper.WaitUntilAsync(
                async () =>
                    await WireMockStubber.VerifyRequest(factory.WireMockBaseUrl, expectedPath, _cancellationToken),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(500),
                _cancellationToken
            );
        }

        releaseProcessed.Should().BeTrue("ReleaseChedReservation should be called within the retry window");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
    }

    public async ValueTask InitializeAsync()
    {
        await _sqsClient.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = QueueUrl }, _cancellationToken);

        await WireMockStubber.ResetAsync(factory.WireMockBaseUrl);

        // The consumer looks up the CHED references for the declaration via the Data API
        // rather than from the resource event body.
        await WireMockStubber.StubDataApiTracesChedsByMrnAsync(factory.WireMockBaseUrl, Mrn, Ched);

        await WireMockStubber.StubChedReleaseAsync(factory.WireMockBaseUrl, Mrn, Ched, CancellationToken.None);

        // After releasing goods, the consumer also syncs reservation state to the Data API; since
        // the Traces Gateway quantities lookup is unstubbed (404), no allocations are returned and
        // the consumer falls back to deleting the reservation in the Data API.
        await WireMockStubber.StubDataApiChedReservationDeleteAsync(
            factory.WireMockBaseUrl,
            Ched,
            Mrn,
            HttpStatusCode.NoContent,
            CancellationToken.None
        );

        await Task.Delay(100, _cancellationToken);
    }
}
