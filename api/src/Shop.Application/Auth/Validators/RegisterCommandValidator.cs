using FluentValidation;
using Shop.Application.Auth.Commands;

namespace Shop.Application.Auth.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("사용자명은 필수입니다.")
            .MinimumLength(3).WithMessage("사용자명은 3자 이상이어야 합니다.")
            .MaximumLength(50).WithMessage("사용자명은 50자 이하여야 합니다.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("이메일은 필수입니다.")
            .EmailAddress().WithMessage("올바른 이메일 형식이 아닙니다.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("비밀번호는 필수입니다.")
            .MinimumLength(6).WithMessage("비밀번호는 6자 이상이어야 합니다.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("이름은 필수입니다.")
            .MaximumLength(50).WithMessage("이름은 50자 이하여야 합니다.");
    }
}
