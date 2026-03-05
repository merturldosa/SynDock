using FluentValidation;
using Shop.Application.Points.Commands;

namespace Shop.Application.Points.Validators;

public class RefundPointsCommandValidator : AbstractValidator<RefundPointsCommand>
{
    public RefundPointsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");
    }
}
