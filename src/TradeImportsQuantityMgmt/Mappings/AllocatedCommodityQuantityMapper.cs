using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Mappings;

public static class AllocatedCommodityQuantityMapper
{
    public static AllocatedCommodityQuantity ToApiContract(
        this Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity source
    )
    {
        return new AllocatedCommodityQuantity
        {
            GoodsItemNumber = source.GoodsItemNumber,
            CommodityCode = source.CommodityCode?.ToApiContract(),
            CertificateLineNumber = source.CertificateLineNumber,
            UnitOfMeasure = source.UnitOfMeasure is null
                ? null
                : Enum.Parse<UniversalUnitOfMeasureType>(source.UnitOfMeasure),
            Quantity = source.Quantity,
            TechnicalRoundingQuantity = source.TechnicalRoundingQuantity,
            EventDateTime = source.EventDateTime,
            CustomsOffice = source.CustomsOffice,
            DeclarationReference = source.DeclarationReference?.ToApiContract(),
        };
    }
}
