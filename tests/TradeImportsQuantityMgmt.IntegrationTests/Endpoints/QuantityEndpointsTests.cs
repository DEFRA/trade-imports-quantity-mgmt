using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Refit;
using Trade.Gateway.Api.Contract.Certificate;
using TradeImportsQuantityMgmt.Contract;
using GatewayChedDeclarationReservation = Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation;
using GatewayChedReservationRequest = Trade.Gateway.Api.Contract.Customs.ChedReservationRequest;

namespace TradeImportsQuantityMgmt.IntegrationTests.Endpoints;

[Collection(IntegrationTestCollection.Name)]
public class QuantityEndpointsTests(TradeGatewayWebApplicationFactory factory, ITestOutputHelper output)
    : IAsyncLifetime
{
    private const string Ched = "CHEDA.GB.2026.0000123";
    private const string Mrn = "26GB16RF3TDPZE7AR2";

    private TextWriter? _originalConsoleOut;
    private TextWriter? _originalConsoleError;

    public ValueTask InitializeAsync()
    {
        _originalConsoleOut = Console.Out;
        _originalConsoleError = Console.Error;

        var writer = new TestOutputWriter(output);
        Console.SetOut(writer);
        Console.SetError(writer);

        // The traces gateway client is a singleton shared across every test in the collection -
        // clear it down so a previous test's setup can't leak into this one.
        factory.TracesGatewayChedClient.ClearSubstitute();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_originalConsoleOut is not null)
            Console.SetOut(_originalConsoleOut);

        if (_originalConsoleError is not null)
            Console.SetError(_originalConsoleError);

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Put_ReturnsTheDeclarationsReservation()
    {
        var gatewayResponse = new GatewayChedDeclarationReservation
        {
            Reserved =
            [
                new Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    UnitOfMeasure = "ASVX",
                    Quantity = 300m,
                },
            ],
            Consumed = [],
        };

        factory
            .TracesGatewayChedClient.PutChedReservation(
                Ched,
                Mrn,
                Arg.Any<GatewayChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<GatewayChedDeclarationReservation>(
                    new HttpResponseMessage(HttpStatusCode.OK),
                    gatewayResponse,
                    new RefitSettings()
                )
            );

        var response = await factory
            .CreateQuantityManagementClient()
            .PutChedReservation(Ched, Mrn, ValidRequest(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response
            .ContentHeaders?.ContentType?.MediaType.Should()
            .Be(MediaTypeAttribute.For<ChedDeclarationReservation>());
        await Verify(response.Content);
    }

    [Fact]
    public async Task Put_ReturnsProblem_WhenTradeGatewayReturnsAnError()
    {
        var gatewayErrorResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        var gatewayError = await ApiException.Create(
            new HttpRequestMessage(
                HttpMethod.Put,
                $"http://traces-gateway/customs/cheds/{Ched}/declarations/{Mrn}/reservation"
            ),
            HttpMethod.Put,
            gatewayErrorResponse,
            new RefitSettings()
        );

        factory
            .TracesGatewayChedClient.PutChedReservation(
                Ched,
                Mrn,
                Arg.Any<GatewayChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new ApiResponse<GatewayChedDeclarationReservation>(
                    gatewayErrorResponse,
                    default!,
                    new RefitSettings(),
                    gatewayError
                )
            );

        var response = await factory
            .CreateQuantityManagementClient()
            .PutChedReservation(Ched, Mrn, ValidRequest(), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Error.Should().NotBeNull();

        var problem = await ((ApiException)response.Error!).GetContentAsAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.NotFound);
        problem.Detail.Should().Be(gatewayError.Message);
    }

    [Fact]
    public async Task Put_ReturnsValidationProblem_WhenRequestIsInvalid()
    {
        var invalidRequest = new ChedReservationRequest { Items = [] };

        var response = await factory
            .CreateQuantityManagementClient()
            .PutChedReservation(Ched, Mrn, invalidRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();

        var problem = await ((ApiException)response.Error!).GetContentAsAsync<HttpValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey(nameof(ChedReservationRequest.Items));

        // Validation should short-circuit before the request ever reaches the gateway.
        await factory
            .TracesGatewayChedClient.DidNotReceive()
            .PutChedReservation(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<GatewayChedReservationRequest>(),
                Arg.Any<CancellationToken>()
            );
    }

    private static ChedReservationRequest ValidRequest() =>
        new()
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    NetWeightQuantity = 300m,
                    NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.ASVX,
                },
            ],
        };
}
