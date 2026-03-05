using FluentValidation;
using Shop.Application.Orders.Commands;

namespace Shop.Application.Orders.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.PointsToUse)
            .GreaterThanOrEqualTo(0).WithMessage("포인트 사용량은 0 이상이어야 합니다.");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("주문 메모는 500자 이하여야 합니다.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("쿠폰 코드는 50자 이하여야 합니다.");
    }
}
