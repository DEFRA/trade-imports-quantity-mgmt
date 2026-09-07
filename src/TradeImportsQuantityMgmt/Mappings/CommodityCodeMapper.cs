using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Mappings;

public static class CommodityCodeMapper
{
    public static CommodityCode ToApiContract(this Trade.Gateway.Api.Contract.Customs.CommodityCode source)
    {
        return new CommodityCode
        {
            HarmonizedSystemSubheadingCode = source.HarmonizedSystemSubheadingCode,
            CombinedNomenclatureCode = source.CombinedNomenclatureCode,
            TaricCode = source.TaricCode,
        };
    }
}
