using FluentValidation;
using Shop.Application.Products.Commands;

namespace Shop.Application.Products.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must be 200 characters or less.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be 0 or greater.");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue)
            .WithMessage("Sale price must be 0 or greater.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category is required.");

        RuleFor(x => x.Description)
            .MaximumLength(10000).WithMessage("Product description must be 10000 characters or less.");

        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug must be 200 characters or less.");

        RuleFor(x => x.Variants)
            .Must(v => v == null || v.Count <= 100)
            .WithMessage("Variants must be 100 or fewer.");
    }
}
