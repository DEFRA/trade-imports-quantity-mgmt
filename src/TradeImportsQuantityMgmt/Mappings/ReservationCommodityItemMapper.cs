using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Mappings;

public static class ReservationCommodityItemMapper
{
    public static Trade.Gateway.Api.Contract.Customs.ReservationCommodityItem ToTradeGatewayDto(
        this ReservationCommodityItem source
    )
    {
        return new Trade.Gateway.Api.Contract.Customs.ReservationCommodityItem
        {
            GoodsItemNumber = source.GoodsItemNumber,
            CertificateLineNumber = source.CertificateLineNumber,
            ClassCode = source.ClassCode,
            NetWeightQuantity = source.NetWeightQuantity,
            NetWeightUnitOfMeasure = source.NetWeightUnitOfMeasure?.ToString(),
            NetVolumeQuantity = source.NetVolumeQuantity,
            NetVolumeUnitOfMeasure = source.NetVolumeUnitOfMeasure?.ToString(),
        };
    }
}
