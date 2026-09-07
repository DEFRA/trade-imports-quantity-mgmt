using AwesomeAssertions;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class ChedDeclarationReservationMapperTests
{
    [Fact]
    public void ToApiContract_MapsReservedAndConsumed()
    {
        var source = new Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation
        {
            Reserved =
            [
                new Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity
                {
                    GoodsItemNumber = 1,
                    UnitOfMeasure = "ASVX",
                    Quantity = 300m,
                },
            ],
            Consumed =
            [
                new Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity
                {
                    GoodsItemNumber = 2,
                    UnitOfMeasure = "KGM",
                    Quantity = 50m,
                },
            ],
        };

        var result = source.ToApiContract();

        result.Reserved.Should().ContainSingle();
        result.Reserved[0].GoodsItemNumber.Should().Be(1);
        result.Reserved[0].UnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.ASVX);

        result.Consumed.Should().ContainSingle();
        result.Consumed[0].GoodsItemNumber.Should().Be(2);
        result.Consumed[0].UnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.KGM);
    }

    [Fact]
    public void ToApiContract_MapsEmptyArrays()
    {
        var source = new Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation { Reserved = [], Consumed = [] };

        var result = source.ToApiContract();

        result.Reserved.Should().BeEmpty();
        result.Consumed.Should().BeEmpty();
    }
}
