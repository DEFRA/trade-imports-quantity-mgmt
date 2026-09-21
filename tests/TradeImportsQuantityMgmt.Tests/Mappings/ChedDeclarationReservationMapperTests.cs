using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.Traces;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;
using AllocatedCommodityQuantity = Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity;
using DeclarationReference = Trade.Gateway.Api.Contract.Customs.DeclarationReference;
using DeclarationReferenceType = Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType;
using QuantityAllocations = Trade.Gateway.Api.Contract.Customs.QuantityAllocations;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class ChedDeclarationReservationMapperTests
{
    private const string ChedId = "CHEDA.GB.2026.0000123";
    private const string ThisMrn = "26GBTHISDECL";
    private const string OtherMrn = "99GBOTHERDECL";

    private static AllocatedCommodityQuantity AllocationFor(
        string mrn,
        decimal quantity,
        DateTimeOffset? eventDateTime = null
    ) =>
        new()
        {
            GoodsItemNumber = 1,
            UnitOfMeasure = "ASVX",
            Quantity = quantity,
            EventDateTime = eventDateTime,
            DeclarationReference = new DeclarationReference { Type = DeclarationReferenceType.Mrn, Value = mrn },
        };

    private static Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation ReservedWithCommodityCode(
        Trade.Gateway.Api.Contract.Customs.CommodityCode? commodityCode
    ) =>
        new()
        {
            Reserved =
            [
                new AllocatedCommodityQuantity
                {
                    GoodsItemNumber = 1,
                    UnitOfMeasure = "ASVX",
                    Quantity = 10m,
                    CommodityCode = commodityCode,
                },
            ],
            Consumed = [],
        };

    [Fact]
    public void ToReservationForDataApi_ChedDeclarationReservation_CommodityCodeUsesTaricCodeWhenPresent()
    {
        var source = ReservedWithCommodityCode(
            new Trade.Gateway.Api.Contract.Customs.CommodityCode
            {
                TaricCode = "0123456789",
                HarmonizedSystemSubheadingCode = "012345",
            }
        );

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Commodities.Should().ContainSingle().Which.CommodityCode.Should().Be("0123456789");
    }

    [Fact]
    public void ToReservationForDataApi_ChedDeclarationReservation_CommodityCodeFallsBackToHarmonizedSystemSubheadingCode()
    {
        var source = ReservedWithCommodityCode(
            new Trade.Gateway.Api.Contract.Customs.CommodityCode
            {
                TaricCode = null,
                HarmonizedSystemSubheadingCode = "012345",
            }
        );

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Commodities.Should().ContainSingle().Which.CommodityCode.Should().Be("012345");
    }

    [Fact]
    public void ToReservationForDataApi_ChedDeclarationReservation_CommodityCodeIsUnknownWhenNoCodesPresent()
    {
        var source = ReservedWithCommodityCode(new Trade.Gateway.Api.Contract.Customs.CommodityCode());

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Commodities.Should().ContainSingle().Which.CommodityCode.Should().Be("Unknown");
    }

    [Fact]
    public void ToReservationForDataApi_ChedDeclarationReservation_CommodityCodeIsUnknownWhenCommodityCodeIsNull()
    {
        var source = ReservedWithCommodityCode(null);

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Commodities.Should().ContainSingle().Which.CommodityCode.Should().Be("Unknown");
    }

    [Fact]
    public void ToReservationForDataApi_QuantityAllocations_IgnoresOtherDeclarationsOnTheSameChed()
    {
        // Allocations is the whole-CHED ledger: another declaration on the same CHED holds a
        // reservation, while this declaration's own allocation has been consumed. Without
        // filtering by MRN, the other declaration's "Reserved" row would win and be persisted
        // as this declaration's status.
        var source = new QuantityAllocations
        {
            Reserved = [AllocationFor(OtherMrn, 111m)],
            Consumed = [AllocationFor(ThisMrn, 300m)],
        };

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Status.Should().Be(ReservationStatus.Consumed);
        result.Commodities.Should().ContainSingle();
        result.Commodities[0].Quantity.Should().Be(300m);
    }

    [Fact]
    public void ToReservationForDataApi_QuantityAllocations_ReservedTakesPrecedenceOverConsumed()
    {
        var source = new QuantityAllocations
        {
            Reserved = [AllocationFor(ThisMrn, 100m)],
            Consumed = [AllocationFor(ThisMrn, 50m)],
        };

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Status.Should().Be(ReservationStatus.Reserved);
        result.Commodities.Should().ContainSingle();
        result.Commodities[0].Quantity.Should().Be(100m);
    }

    [Fact]
    public void ToReservationForDataApi_QuantityAllocations_ConsumedOnlyMapsToConsumed()
    {
        var source = new QuantityAllocations { Reserved = [], Consumed = [AllocationFor(ThisMrn, 50m)] };

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Status.Should().Be(ReservationStatus.Consumed);
        result.Commodities.Should().ContainSingle();
    }

    [Fact]
    public void ToReservationForDataApi_QuantityAllocations_NoAllocationsForThisDeclarationMapsToUnreserved()
    {
        // Only another declaration's rows exist on the CHED - nothing for this MRN once filtered.
        var source = new QuantityAllocations { Reserved = [AllocationFor(OtherMrn, 111m)], Consumed = [] };

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Status.Should().Be(ReservationStatus.Unreserved);
        result.Commodities.Should().BeEmpty();
    }

    [Fact]
    public void ToReservationForDataApi_QuantityAllocations_UsesLatestEventTimeAcrossMatchedCommodities()
    {
        var earlier = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var later = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var source = new QuantityAllocations
        {
            Reserved = [AllocationFor(ThisMrn, 10m, earlier), AllocationFor(ThisMrn, 20m, later)],
            Consumed = [],
        };

        var result = source.ToReservationForDataApi(ChedId, ThisMrn);

        result.Timestamp.Should().Be(later.UtcDateTime);
    }

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
