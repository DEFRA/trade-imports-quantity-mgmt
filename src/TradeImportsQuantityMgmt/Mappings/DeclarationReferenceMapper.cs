using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Mappings;

public static class DeclarationReferenceMapper
{
    public static DeclarationReference ToApiContract(
        this Trade.Gateway.Api.Contract.Customs.DeclarationReference source
    )
    {
        return new DeclarationReference
        {
            Type = Enum.Parse<DeclarationReferenceType>(source.Type.ToString()),
            Value = source.Value,
        };
    }
}
