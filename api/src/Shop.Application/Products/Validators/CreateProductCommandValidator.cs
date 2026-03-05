using FluentValidation;
using Shop.Application.Products.Commands;

namespace Shop.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("상품명은 필수입니다.")
            .MaximumLength(200).WithMessage("상품명은 200자 이하여야 합니다.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("가격은 0 이상이어야 합니다.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue)
            .WithMessage("할인가는 0 이상이어야 합니다.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("카테고리는 필수입니다.");

        RuleFor(x => x.Description)
            .MaximumLength(10000).WithMessage("상품 설명은 10000자 이하여야 합니다.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("슬러그는 200자 이하여야 합니다.");

        RuleFor(x => x.Variants)
            .Must(v => v == null || v.Count <= 100)
            .WithMessage("변형은 100개 이하여야 합니다.");
    }
}
