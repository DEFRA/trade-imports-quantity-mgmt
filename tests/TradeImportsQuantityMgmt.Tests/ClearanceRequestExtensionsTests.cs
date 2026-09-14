using AwesomeAssertions;
using Defra.TradeImportsDataApi.Domain.CustomsDeclaration;
using TradeImportsQuantityMgmt.Features.ResourceEvents;

namespace TradeImportsQuantityMgmt.Tests;

public class ClearanceRequestExtensionsTests
{
    [Fact]
    public void GetTracesCheds_ReturnsDistinctReferences_AndHandlesNulls()
    {
        var doc1 = new ImportDocument
        {
            DocumentCode = "9115",
            DocumentReference = new ImportDocumentReference("CHEDA.GB.2026.1234567"),
        };

        var doc2 = new ImportDocument
        {
            DocumentCode = "9115",
            DocumentReference = new ImportDocumentReference("CHEDA.GB.2026.7654321"),
        };

        var commodity1 = new Commodity { Documents = new[] { doc1 } };
        var commodity2 = new Commodity { Documents = new[] { doc1 } }; // duplicate reference
        var commodity3 = new Commodity { Documents = new[] { doc2 } };

        var clearance = new ClearanceRequest { Commodities = new[] { commodity1, commodity2, commodity3 } };

        var traces = clearance.GetTracesCheds().ToArray();

        traces.Length.Should().Be(2);
        traces.Should().Contain("CHEDA.GB.2026.1234567");
        traces.Should().Contain("CHEDA.GB.2026.7654321");
    }

    [Fact]
    public void GetTracesCheds_ReturnsEmpty_ForNullClearanceRequest()
    {
        ClearanceRequest? clearance = null;

        var traces = clearance.GetTracesCheds();

        traces.Should().BeEmpty();
    }
}
