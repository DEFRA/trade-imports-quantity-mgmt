using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Mappings;

public static class ChedReservationRequestMapper
{
    public static Trade.Gateway.Api.Contract.Customs.ChedReservationRequest ToTradeGatewayDto(
        this ChedReservationRequest source
    )
    {
        return new Trade.Gateway.Api.Contract.Customs.ChedReservationRequest
        {
            Items = source.Items.Select(x => x.ToTradeGatewayDto()).ToArray(),
        };
    }
}
