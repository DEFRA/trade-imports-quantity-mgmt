using AwesomeAssertions;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class CommodityCodeMapperTests
{
    [Fact]
    public void ToApiContract_MapsAllFields()
    {
        var source = new Trade.Gateway.Api.Contract.Customs.CommodityCode
        {
            HarmonizedSystemSubheadingCode = "123456",
            CombinedNomenclatureCode = "12345678",
            TaricCode = "1234567890",
        };

        var result = source.ToApiContract();

        result.HarmonizedSystemSubheadingCode.Should().Be("123456");
        result.CombinedNomenclatureCode.Should().Be("12345678");
        result.TaricCode.Should().Be("1234567890");
    }
}
