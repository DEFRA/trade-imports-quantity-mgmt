using System.Net;
using AwesomeAssertions;
using TradeImportsQuantityMgmt.Features.QuantityManagement;

namespace TradeImportsQuantityMgmt.Tests;

public class QuantityManagementOutcomeTests
{
    private const string Ched = "CHEDA.GB.2026.1234567";
    private const string Mrn = "25GBVLKTCO0HN7MUA4";

    [Fact]
    public void FromResponse_IsSuccess_WhenStatusCodeIsSuccessful()
    {
        var outcome = QuantityManagementOutcome.FromResponse(
            QuantityManagementOperation.PutReservation,
            Ched,
            Mrn,
            HttpStatusCode.OK,
            """{"reason":"IGNORED"}"""
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Outcome.Should().Be(QuantityManagementOutcome.SuccessOutcome);
        outcome.Reason.Should().BeNull();
        outcome.Detail.Should().BeNull();
    }

    [Fact]
    public void FromResponse_UsesProblemReasonAndDetail_WhenUnsuccessful()
    {
        var outcome = QuantityManagementOutcome.FromResponse(
            QuantityManagementOperation.PutReservation,
            Ched,
            Mrn,
            HttpStatusCode.Conflict,
            """{"title":"Conflict","Detail":"Not enough quantity","Reason":"INSUFFICIENT_QUANTITY"}"""
        );

        outcome.IsSuccess.Should().BeFalse();
        outcome.Outcome.Should().Be("INSUFFICIENT_QUANTITY");
        outcome.Reason.Should().Be("INSUFFICIENT_QUANTITY");
        outcome.Detail.Should().Be("Not enough quantity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"reason":42}""")]
    public void FromResponse_IsUnknown_WhenUnsuccessfulWithoutReason(string? content)
    {
        var outcome = QuantityManagementOutcome.FromResponse(
            QuantityManagementOperation.ReleaseReservation,
            Ched,
            Mrn,
            HttpStatusCode.InternalServerError,
            content
        );

        outcome.Outcome.Should().Be(QuantityManagementOutcome.UnknownOutcome);
        outcome.Reason.Should().BeNull();
    }

    [Fact]
    public void Record_EmitsOutcomeMetric()
    {
        using var capture = new QuantityManagementOutcomeCapture();

        capture.Recorder.Record(
            QuantityManagementOutcome.FromResponse(
                QuantityManagementOperation.DeleteReservation,
                Ched,
                Mrn,
                HttpStatusCode.NotFound,
                """{"reason":"RESERVATION_NOT_FOUND"}"""
            )
        );

        capture
            .Measurements.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new OutcomeMeasurement("DeleteReservation", "RESERVATION_NOT_FOUND", "404"));
    }
}
