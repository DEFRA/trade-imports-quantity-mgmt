using Refit;
using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Client.Clients;

public interface IQuantityManagementClient
{
    [Get("/health")]
    Task<HttpResponseMessage> HealthCheck(CancellationToken cancellationToken);

    [Put("/customs/cheds/{id}/declarations/{mrn}/reservation")]
    Task<ApiResponse<ChedDeclarationReservation>> PutChedReservation(
        string id,
        string mrn,
        [Body] ChedReservationRequest request,
        CancellationToken cancellationToken
    );
}
