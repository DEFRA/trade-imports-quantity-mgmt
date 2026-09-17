using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using Defra.TradeImportsDataApi.Domain.Events;
using Defra.TradeImportsDataApi.Domain.Ipaffs;
using NSubstitute;
using NSubstitute.ClearExtensions;

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
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                ),
                MessageAttributes = messageAttributes,
            },
            _cancellationToken
        );

        var expectedPath = $"/customs/cheds/{Ched}/declarations/{Mrn}/reservation/release";
        var releaseProcessed = await WaitHelper.WaitUntilAsync(
            async () => await WireMockStubber.VerifyRequest(factory.WireMockBaseUrl, expectedPath, _cancellationToken),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(500),
            _cancellationToken
        );

        releaseProcessed.Should().BeTrue("ReleaseChedReservation should be called within 60s");
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
    }

    public async ValueTask InitializeAsync()
    {
        await _sqsClient.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = QueueUrl }, _cancellationToken);

        await WireMockStubber.ResetAsync(factory.WireMockBaseUrl);
        await WireMockStubber.StubChedReleaseAsync(factory.WireMockBaseUrl, Mrn, Ched, CancellationToken.None);

        // The consumer also syncs the reservation state to the Data API after releasing goods; since the
        // Traces Gateway quantities lookup is unstubbed (404), no allocations are returned and the consumer
        // falls back to deleting the reservation in the Data API.
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
