using AwesomeAssertions;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Mappings;

namespace TradeImportsQuantityMgmt.Tests.Mappings;

public class DeclarationReferenceMapperTests
{
    [Theory]
    [InlineData(Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType.Mrn, DeclarationReferenceType.Mrn)]
    [InlineData(Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType.Lrn, DeclarationReferenceType.Lrn)]
    public void ToApiContract_MapsType(
        Trade.Gateway.Api.Contract.Customs.DeclarationReferenceType source,
        DeclarationReferenceType expected
    )
    {
        var result = new Trade.Gateway.Api.Contract.Customs.DeclarationReference
        {
            Type = source,
            Value = "26GB16RF3TDPZE7AR2",
        }.ToApiContract();

        result.Type.Should().Be(expected);
        result.Value.Should().Be("26GB16RF3TDPZE7AR2");
    }
}
