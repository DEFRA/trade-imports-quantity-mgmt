using AwesomeAssertions;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class ReservationCommodityItemMapperTests
{
    [Fact]
    public void ToTradeGatewayDto_MapsAllFields()
    {
        var source = new ReservationCommodityItem
        {
            GoodsItemNumber = 1,
            CertificateLineNumber = 2,
            ClassCode = "P1",
            NetWeightQuantity = 300m,
            NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
            NetVolumeQuantity = 10m,
            NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.LTR,
        };

        var result = source.ToTradeGatewayDto();

        result.GoodsItemNumber.Should().Be(1);
        result.CertificateLineNumber.Should().Be(2);
        result.ClassCode.Should().Be("P1");
        result.NetWeightQuantity.Should().Be(300m);
        result.NetWeightUnitOfMeasure.Should().Be("KGM");
        result.NetVolumeQuantity.Should().Be(10m);
        result.NetVolumeUnitOfMeasure.Should().Be("LTR");
    }

    [Fact]
    public void ToTradeGatewayDto_LeavesUnsuppliedUnitsNull()
    {
        var source = new ReservationCommodityItem
        {
            GoodsItemNumber = 1,
            CertificateLineNumber = 2,
            ClassCode = "P1",
            NetWeightQuantity = 300m,
            NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
        };

        var result = source.ToTradeGatewayDto();

        result.NetVolumeQuantity.Should().BeNull();
        result.NetVolumeUnitOfMeasure.Should().BeNull();
    }
}
