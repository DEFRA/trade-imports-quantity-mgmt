using FluentValidation.TestHelper;
using TradeImportsQuantityMgmt.Contract;
using TradeImportsQuantityMgmt.Validation;

namespace TradeImportsQuantityMgmt.Tests.Validation;

public class ChedReservationRequestValidatorTests
{
    private readonly ChedReservationRequestValidator _validator = new();

    private static ReservationCommodityItem ValidItem() =>
        new()
        {
            GoodsItemNumber = 1,
            CertificateLineNumber = 1,
            ClassCode = "P1",
            NetWeightQuantity = 300m,
            NetWeightUnitOfMeasure = UniversalUnitOfMeasureType.KGM,
        };

    [Fact]
    public void ValidRequest_HasNoValidationErrors()
    {
        var request = new ChedReservationRequest { Items = [ValidItem()] };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyItems_HasValidationErrorForItems()
    {
        var request = new ChedReservationRequest { Items = [] };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Items);
    }

    [Fact]
    public void MissingGoodsItemNumber_HasValidationError()
    {
        var request = new ChedReservationRequest { Items = [ValidItem() with { GoodsItemNumber = null }] };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].GoodsItemNumber");
    }

    [Fact]
    public void MissingCertificateLineNumber_HasValidationError()
    {
        var request = new ChedReservationRequest { Items = [ValidItem() with { CertificateLineNumber = null }] };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].CertificateLineNumber");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MissingClassCode_HasValidationError(string? classCode)
    {
        var request = new ChedReservationRequest { Items = [ValidItem() with { ClassCode = classCode }] };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].ClassCode");
    }

    [Fact]
    public void NeitherWeightNorVolumeSupplied_HasValidationError()
    {
        var request = new ChedReservationRequest
        {
            Items = [ValidItem() with { NetWeightQuantity = null, NetWeightUnitOfMeasure = null }],
        };

        var result = _validator.TestValidate(request);

        result
            .ShouldHaveValidationErrorFor("Items[0]")
            .WithErrorMessage("netWeightQuantity or netVolumeQuantity is required.");
    }

    [Fact]
    public void VolumeAloneSatisfiesTheQuantityRequirement()
    {
        var request = new ChedReservationRequest
        {
            Items =
            [
                ValidItem() with
                {
                    NetWeightQuantity = null,
                    NetWeightUnitOfMeasure = null,
                    NetVolumeQuantity = 10m,
                    NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.LTR,
                },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NegativeNetWeightQuantity_HasValidationError()
    {
        var request = new ChedReservationRequest { Items = [ValidItem() with { NetWeightQuantity = -1m }] };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].NetWeightQuantity");
    }

    [Fact]
    public void NegativeNetVolumeQuantity_HasValidationError()
    {
        var request = new ChedReservationRequest
        {
            Items =
            [
                ValidItem() with
                {
                    NetVolumeQuantity = -1m,
                    NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.LTR,
                },
            ],
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].NetVolumeQuantity");
    }

    [Fact]
    public void NetWeightQuantityWithoutUnitOfMeasure_HasValidationError()
    {
        var request = new ChedReservationRequest { Items = [ValidItem() with { NetWeightUnitOfMeasure = null }] };

        var result = _validator.TestValidate(request);

        result
            .ShouldHaveValidationErrorFor("Items[0].NetWeightUnitOfMeasure")
            .WithErrorMessage("'' is not a recognised unit of measure.");
    }

    [Fact]
    public void NetVolumeQuantityWithoutUnitOfMeasure_HasValidationError()
    {
        var request = new ChedReservationRequest
        {
            Items = [ValidItem() with { NetVolumeQuantity = 10m, NetVolumeUnitOfMeasure = null }],
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Items[0].NetVolumeUnitOfMeasure");
    }

    [Fact]
    public void UnitOfMeasureWithoutItsQuantity_IsNotValidated()
    {
        // NetVolumeUnitOfMeasure is only required once NetVolumeQuantity is supplied.
        var request = new ChedReservationRequest
        {
            Items = [ValidItem() with { NetVolumeUnitOfMeasure = UniversalUnitOfMeasureType.LTR }],
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Items[0].NetVolumeUnitOfMeasure");
    }
}
