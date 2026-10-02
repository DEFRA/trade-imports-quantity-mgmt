using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
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
            null
        );

        outcome.IsSuccess.Should().BeTrue();
        outcome.Outcome.Should().Be(QuantityManagementOutcome.SuccessOutcome);
        outcome.Reason.Should().BeNull();
        outcome.Detail.Should().BeNull();
    }

    [Fact]
    public void FromResponse_IsUnsuccessful_WithProblemReasonAndDetail()
    {
        var outcome = QuantityManagementOutcome.FromResponse(
            QuantityManagementOperation.PutReservation,
            Ched,
            Mrn,
            HttpStatusCode.Conflict,
            ParseProblem("""{"title":"Conflict","detail":"Not enough quantity","reason":"QuantitiesInsufficient"}""")
        );

        outcome.IsSuccess.Should().BeFalse();
        outcome.Outcome.Should().Be(QuantityManagementOutcome.UnsuccessfulOutcome);
        outcome.Reason.Should().Be("QuantitiesInsufficient");
        outcome.Detail.Should().Be("Not enough quantity");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("""{"title":"Server error"}""")]
    [InlineData("""{"reason":42}""")]
    public void FromResponse_HasNoReason_WhenProblemDoesNotSupplyOne(string? problemJson)
    {
        var outcome = QuantityManagementOutcome.FromResponse(
            QuantityManagementOperation.ReleaseReservation,
            Ched,
            Mrn,
            HttpStatusCode.InternalServerError,
            problemJson is null ? null : ParseProblem(problemJson)
        );

        outcome.Outcome.Should().Be(QuantityManagementOutcome.UnsuccessfulOutcome);
        outcome.Reason.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, null, "Success", "None")]
    [InlineData(HttpStatusCode.Conflict, "QuantitiesInsufficient", "Unsuccessful", "QuantitiesInsufficient")]
    [InlineData(HttpStatusCode.InternalServerError, null, "Unsuccessful", "Unknown")]
    public void Record_EmitsOutcomeMetric(
        HttpStatusCode statusCode,
        string? reason,
        string expectedOutcome,
        string expectedReason
    )
    {
        using var capture = new QuantityManagementOutcomeCapture();

        capture.Recorder.Record(
            new QuantityManagementOutcome(
                QuantityManagementOperation.DeleteReservation,
                Ched,
                Mrn,
                statusCode,
                reason,
                null
            )
        );

        capture
            .Measurements.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new OutcomeMeasurement(
                    "DeleteReservation",
                    expectedOutcome,
                    expectedReason,
                    ((int)statusCode).ToString()
                )
            );
    }

    private static ProblemDetails ParseProblem(string json) =>
        JsonSerializer.Deserialize<ProblemDetails>(json, JsonSerializerOptions.Web)!;
}
