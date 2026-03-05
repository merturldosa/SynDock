using FluentValidation;
using Shop.Application.Platform.Commands;

namespace Shop.Application.Platform.Validators;

public class UpsertCommissionSettingCommandValidator : AbstractValidator<UpsertCommissionSettingCommand>
{
    public UpsertCommissionSettingCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("Tenant ID is required.");

        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0, 100).WithMessage("Commission rate must be between 0 and 100%.");

        RuleFor(x => x.SettlementCycle)
            .NotEmpty().WithMessage("Settlement cycle is required.")
            .MaximumLength(50).WithMessage("Settlement cycle must be 50 characters or less.");

        RuleFor(x => x.SettlementDayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Settlement day of week must be between 0 (Sun) and 6 (Sat).");

        RuleFor(x => x.MinSettlementAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum settlement amount must be 0 or greater.");

        RuleFor(x => x.BankName)
            .MaximumLength(50).When(x => x.BankName is not null)
            .WithMessage("Bank name must be 50 characters or less.");

        RuleFor(x => x.BankAccount)
            .MaximumLength(50).When(x => x.BankAccount is not null)
            .WithMessage("Bank account must be 50 characters or less.");

        RuleFor(x => x.BankHolder)
            .MaximumLength(50).When(x => x.BankHolder is not null)
            .WithMessage("Account holder must be 50 characters or less.");
    }
}
