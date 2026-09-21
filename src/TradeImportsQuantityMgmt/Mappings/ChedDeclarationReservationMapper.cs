using Defra.TradeImportsDataApi.Domain.Traces;
using TradeImportsQuantityMgmt.Contract;
using AllocatedCommodityQuantity = Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity;
using DeclarationReferenceType = Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType;

namespace TradeImportsQuantityMgmt.Mappings;

public static class ChedDeclarationReservationMapper
{
    public static ChedDeclarationReservation ToApiContract(
        this Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation source
    )
    {
        return new ChedDeclarationReservation
        {
            Reserved = source.Reserved.Select(x => x.ToApiContract()).ToArray(),
            Consumed = source.Consumed.Select(x => x.ToApiContract()).ToArray(),
        };
    }

    public static Reservation ToReservationForDataApi(
        this Trade.Gateway.Api.Contract.Customs.ChedDeclarationReservation source,
        string chedId,
        string mrn
    )
    {
        return ToReservationForDataApi(chedId, mrn, source.Reserved ?? [], source.Consumed ?? []);
    }

    /// <remarks>
    /// <paramref name="source"/> is the whole-CHED ledger: every declaration's allocations against
    /// this CHED, not just this MRN's. It must be filtered down to <paramref name="mrn"/> before
    /// mapping, otherwise another declaration's reservation/consumption on the same CHED can be
    /// persisted as this one's.
    /// </remarks>
    public static Reservation ToReservationForDataApi(
        this Trade.Gateway.Api.Contract.Customs.QuantityAllocations source,
        string chedId,
        string mrn
    )
    {
        return ToReservationForDataApi(
            chedId,
            mrn,
            FilterByMrn(source.Reserved, mrn),
            FilterByMrn(source.Consumed, mrn)
        );
    }

    private static AllocatedCommodityQuantity[] FilterByMrn(AllocatedCommodityQuantity[]? source, string mrn)
    {
        if (source is null)
            return [];

        return source
            .Where(x =>
                x.DeclarationReference is { Type: DeclarationReferenceType.Mrn } reference && reference.Value == mrn
            )
            .ToArray();
    }

    private static Reservation ToReservationForDataApi(
        string chedId,
        string mrn,
        AllocatedCommodityQuantity[] reserved,
        AllocatedCommodityQuantity[] consumed
    )
    {
        var (status, allocations) = SelectAllocations(reserved, consumed);

        return new Reservation()
        {
            ChedId = chedId,
            Mrn = mrn,
            Status = status,
            Timestamp = LatestEventTimestamp(allocations),
            Commodities = allocations.Select(ToReservationCommodity).ToArray(),
        };
    }

    private static (string Status, AllocatedCommodityQuantity[] Allocations) SelectAllocations(
        AllocatedCommodityQuantity[] reserved,
        AllocatedCommodityQuantity[] consumed
    )
    {
        if (reserved.Any())
            return (ReservationStatus.Reserved, reserved);

        if (consumed.Any())
            return (ReservationStatus.Consumed, consumed);

        return (ReservationStatus.Unreserved, []);
    }

    private static DateTime LatestEventTimestamp(AllocatedCommodityQuantity[] allocations)
    {
        var latest = allocations
            .Select(x => x.EventDateTime)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .DefaultIfEmpty(DateTimeOffset.UtcNow)
            .Max();

        return latest.UtcDateTime;
    }

    private static ReservationCommodity ToReservationCommodity(AllocatedCommodityQuantity x) =>
        new()
        {
            CertificateLineNumber = x.CertificateLineNumber.GetValueOrDefault(),
            CommodityCode = x.CommodityCode?.TaricCode ?? x.CommodityCode?.HarmonizedSystemSubheadingCode ?? "Unknown",
            GoodsItemNumber = x.GoodsItemNumber.GetValueOrDefault(),
            Quantity = x.Quantity,
            UnitOfMeasure = x.UnitOfMeasure!,
        };
}
