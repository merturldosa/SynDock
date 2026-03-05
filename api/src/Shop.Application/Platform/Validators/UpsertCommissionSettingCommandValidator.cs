using FluentValidation;
using Shop.Application.Platform.Commands;

namespace Shop.Application.Platform.Validators;

public class UpsertCommissionSettingCommandValidator : AbstractValidator<UpsertCommissionSettingCommand>
{
    public UpsertCommissionSettingCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("테넌트 ID는 필수입니다.");

        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0, 100).WithMessage("수수료율은 0~100% 사이여야 합니다.");

        RuleFor(x => x.SettlementCycle)
            .NotEmpty().WithMessage("정산 주기는 필수입니다.")
            .MaximumLength(50).WithMessage("정산 주기는 50자 이하여야 합니다.");

        RuleFor(x => x.SettlementDayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("정산 요일은 0(일)~6(토) 사이여야 합니다.");

        RuleFor(x => x.MinSettlementAmount)
            .GreaterThanOrEqualTo(0).WithMessage("최소 정산 금액은 0 이상이어야 합니다.");

        RuleFor(x => x.BankName)
            .MaximumLength(50).When(x => x.BankName is not null)
            .WithMessage("은행명은 50자 이하여야 합니다.");

        RuleFor(x => x.BankAccount)
            .MaximumLength(50).When(x => x.BankAccount is not null)
            .WithMessage("계좌번호는 50자 이하여야 합니다.");

        RuleFor(x => x.BankHolder)
            .MaximumLength(50).When(x => x.BankHolder is not null)
            .WithMessage("예금주는 50자 이하여야 합니다.");
    }
}
