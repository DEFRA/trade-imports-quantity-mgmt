using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using TradeImportsQuantityMgmt.Features.ResourceEvents;

namespace TradeImportsQuantityMgmt.Tests;

public class CommodityExtensionsTests
{
    [Fact]
    public void IsTracesChed_ReturnsTrue_ForValidChedReference()
    {
        var doc = new ImportDocument
        {
            DocumentCode = "9115",
            DocumentReference = new ImportDocumentReference("CHEDA.GB.2026.1234567"),
        };

        var result = doc.IsTracesChed();

        result.Should().BeTrue();
    }

    [Fact]
    public void GetChedNumber_ReturnsDocumentReference_WhenPresent()
    {
        var doc = new ImportDocument
        {
            DocumentCode = "9115",
            DocumentReference = new ImportDocumentReference("CHEDA.GB.2026.1234567"),
        };

        var commodity = new Commodity { Documents = new[] { doc } };

        var ched = commodity.GetChedNumber();

        ched.Should().Be("CHEDA.GB.2026.1234567");
    }
}
