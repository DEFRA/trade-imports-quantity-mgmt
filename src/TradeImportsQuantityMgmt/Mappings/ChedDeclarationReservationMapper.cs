using TradeImportsQuantityMgmt.Contract;

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
}
