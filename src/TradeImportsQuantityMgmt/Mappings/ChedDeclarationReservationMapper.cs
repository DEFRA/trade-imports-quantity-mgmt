using Defra.TradeImportsDataApi.Domain.Traces;
using TradeImportsQuantityMgmt.Contract;
using AllocatedCommodityQuantity = Trade.Gateway.Api.Contract.Customs.AllocatedCommodityQuantity;

namespace TradeImportsQuantityMgmt.Mappings;

public static class ReservationStatus
{
    public const string Unreserved = nameof(Unreserved);
    public const string Reserved = nameof(Reserved);
    public const string Consumed = nameof(Consumed);
}

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
        return ToReservationForDataApi(chedId, mrn, source.Reserved, source.Consumed);
    }

    public static Reservation ToReservationForDataApi(
        this Trade.Gateway.Api.Contract.Customs.QuantityAllocations source,
        string chedId,
        string mrn
    )
    {
        return ToReservationForDataApi(chedId, mrn, source.Reserved, source.Consumed);
    }

    private static Reservation ToReservationForDataApi(
        string chedId,
        string mrn,
        AllocatedCommodityQuantity[] reserved,
        AllocatedCommodityQuantity[] consumed
    )
    {
        var s = ReservationStatus.Unreserved;
        var commodities = Array.Empty<ReservationCommodity>();
        var ts = DateTimeOffset.UtcNow;
        if (reserved is not null && reserved.Any())
        {
            s = ReservationStatus.Reserved;
            var dateTimeOffset = reserved[0].EventDateTime;
            if (dateTimeOffset != null)
                ts = dateTimeOffset.Value;
            commodities = reserved
                .Select(x => new ReservationCommodity()
                {
                    CertificateLineNumber = x.CertificateLineNumber.GetValueOrDefault(),
                    CommodityCode = x.CommodityCode?.TaricCode!,
                    GoodsItemNumber = x.GoodsItemNumber.GetValueOrDefault(),
                    Quantity = x.Quantity,
                    UnitOfMeasure = x.UnitOfMeasure!,
                })
                .ToArray();
        }
        else if (consumed is not null && consumed.Any())
        {
            s = ReservationStatus.Consumed;
            var dateTimeOffset = consumed[0].EventDateTime;
            if (dateTimeOffset != null)
                ts = dateTimeOffset.Value;
            commodities = consumed
                .Select(x => new ReservationCommodity()
                {
                    CertificateLineNumber = x.CertificateLineNumber.GetValueOrDefault(),
                    CommodityCode = x.CommodityCode?.TaricCode!,
                    GoodsItemNumber = x.GoodsItemNumber.GetValueOrDefault(),
                    Quantity = x.Quantity,
                    UnitOfMeasure = x.UnitOfMeasure!,
                })
                .ToArray();
        }

        return new Reservation()
        {
            ChedId = chedId,
            Mrn = mrn,
            Status = s,
            Timestamp = ts.UtcDateTime,
            Commodities = commodities,
        };
    }
}
