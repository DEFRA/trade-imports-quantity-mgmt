using System.Linq.Expressions;
using FluentValidation;
using TradeImportsQuantityMgmt.Contract;

namespace TradeImportsQuantityMgmt.Validation;

public class ChedReservationRequestValidator : AbstractValidator<ChedReservationRequest>
{
    private const string QuantityRequired = "netWeightQuantity or netVolumeQuantity is required.";
    private const string UnrecognisedUnit = "'{PropertyValue}' is not a recognised unit of measure.";

    public ChedReservationRequestValidator()
    {
        RuleFor(request => request.Items).NotEmpty();

        RuleForEach(request => request.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.GoodsItemNumber).NotNull();
                item.RuleFor(i => i.CertificateLineNumber).NotNull();
                item.RuleFor(i => i.ClassCode).NotEmpty();

                item.RuleFor(i => i).Must(HasAQuantity).WithMessage(QuantityRequired);

                item.RuleFor(i => i.NetWeightQuantity).GreaterThanOrEqualTo(0m);
                item.RuleFor(i => i.NetVolumeQuantity).GreaterThanOrEqualTo(0m);

                RequireARecognisedUnit(item, i => i.NetWeightUnitOfMeasure, i => i.NetWeightQuantity is not null);
                RequireARecognisedUnit(item, i => i.NetVolumeUnitOfMeasure, i => i.NetVolumeQuantity is not null);
            });
    }

    private static void RequireARecognisedUnit(
        InlineValidator<ReservationCommodityItem> item,
        Expression<Func<ReservationCommodityItem, UniversalUnitOfMeasureType?>> unitOfMeasure,
        Func<ReservationCommodityItem, bool> whenQuantitySupplied
    ) =>
        item.RuleFor(unitOfMeasure)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .NotNull()
            .WithMessage(UnrecognisedUnit)
            .When(whenQuantitySupplied);

    private static bool HasAQuantity(ReservationCommodityItem item) =>
        item.NetWeightQuantity is not null || item.NetVolumeQuantity is not null;
}
