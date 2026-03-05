using FluentValidation;
using Shop.Application.Orders.Commands;

namespace Shop.Application.Orders.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.PointsToUse)
            .GreaterThanOrEqualTo(0).WithMessage("Points to use must be 0 or greater.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Order note must be 500 characters or less.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Coupon code must be 50 characters or less.");
    }
}
