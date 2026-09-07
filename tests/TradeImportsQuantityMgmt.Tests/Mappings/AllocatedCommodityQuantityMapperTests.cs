using AwesomeAssertions;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class AllocatedCommodityQuantityMapperTests
{
    [Fact]
    public void ToApiContract_MapsAllFieldsIncludingNestedTypes()
    {
        var eventDateTime = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

        var source = new Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity
        {
            GoodsItemNumber = 1,
            CommodityCode = new Trade.Gateway.Api.Contract.Customs.CommodityCode
            {
                HarmonizedSystemSubheadingCode = "123456",
            },
            CertificateLineNumber = 2,
            UnitOfMeasure = "ASVX",
            Quantity = 300m,
            TechnicalRoundingQuantity = 1.5m,
            EventDateTime = eventDateTime,
            CustomsOffice = "GBTEST01",
            DeclarationReference = new Trade.Gateway.Api.Contract.Customs.DeclarationReference
            {
                Type = Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType.Mrn,
                Value = "26GB16RF3TDPZE7AR2",
            },
        };

        var result = source.ToApiContract();

        result.GoodsItemNumber.Should().Be(1);
        result.CommodityCode.Should().NotBeNull();
        result.CommodityCode!.HarmonizedSystemSubheadingCode.Should().Be("123456");
        result.CertificateLineNumber.Should().Be(2);
        result.UnitOfMeasure.Should().Be(UniversalUnitOfMeasureType.ASVX);
        result.Quantity.Should().Be(300m);
        result.TechnicalRoundingQuantity.Should().Be(1.5m);
        result.EventDateTime.Should().Be(eventDateTime);
        result.CustomsOffice.Should().Be("GBTEST01");
        result.DeclarationReference.Should().NotBeNull();
        result.DeclarationReference!.Type.Should().Be(DeclarationReferenceType.Mrn);
        result.DeclarationReference.Value.Should().Be("26GB16RF3TDPZE7AR2");
    }

    [Fact]
    public void ToApiContract_LeavesOptionalNestedTypesNull()
    {
        var source = new Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity
        {
            UnitOfMeasure = null,
            Quantity = 300m,
        };

        var result = source.ToApiContract();

        result.CommodityCode.Should().BeNull();
        result.UnitOfMeasure.Should().BeNull();
        result.DeclarationReference.Should().BeNull();
    }
}
