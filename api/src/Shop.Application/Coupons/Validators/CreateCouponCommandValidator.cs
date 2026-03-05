using FluentValidation;
using Shop.Application.Coupons.Commands;

namespace Shop.Application.Coupons.Validators;

public class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code must be 50 characters or less.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Coupon name is required.")
            .MaximumLength(200).WithMessage("Coupon name must be 200 characters or less.");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than 0.");

        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum order amount must be 0 or greater.");

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue)
            .WithMessage("Maximum discount amount must be greater than 0.");

        RuleFor(x => x.MaxUsageCount)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum usage count must be 0 or greater.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate != default && x.StartDate != default)
            .WithMessage("End date must be after start date.");
    }
}
