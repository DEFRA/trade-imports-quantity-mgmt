using AwesomeAssertions;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class ChedReservationRequestMapperTests
{
    [Fact]
    public void ToTradeGatewayDto_MapsEachItem()
    {
        var source = new ChedReservationRequest
        {
            Items =
            [
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 1,
                    CertificateLineNumber = 1,
                    ClassCode = "P1",
                    NetWeightQuantity = 300m,
                    NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
                },
                new ReservationCommodityItem
                {
                    GoodsItemNumber = 2,
                    CertificateLineNumber = 2,
                    ClassCode = "P2",
                    NetVolumeQuantity = 10m,
                    NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.LTR,
                },
            ],
        };

        var result = source.ToTradeGatewayDto();

        result.Items.Should().HaveCount(2);
        result.Items[0].GoodsItemNumber.Should().Be(1);
        result.Items[0].NetWeightUnitOfMeasure.Should().Be("KGM");
        result.Items[1].GoodsItemNumber.Should().Be(2);
        result.Items[1].NetVolumeUnitOfMeasure.Should().Be("LTR");
    }
}
