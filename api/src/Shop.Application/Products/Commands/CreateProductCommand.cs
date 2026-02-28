using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Products.Commands;

public record CreateProductCommand(
    string Name,
    string? Slug,
    string? Description,
    decimal Price,
    decimal? SalePrice,
    string PriceType,
    string? Specification,
    int CategoryId,
    bool IsFeatured,
    bool IsNew,
    string? CustomFieldsJson,
    List<CreateProductImageDto>? Images,
    List<CreateProductVariantDto>? Variants
) : IRequest<Result<int>>;

public record CreateProductImageDto(string Url, string? AltText, int SortOrder, bool IsPrimary);
public record CreateProductVariantDto(string Name, string? Sku, decimal? Price, int Stock, bool IsActive);

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        // Validate category exists
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);

        if (!categoryExists)
            return Result<int>.Failure("카테고리를 찾을 수 없습니다.");

        // Check slug uniqueness if provided
        if (!string.IsNullOrEmpty(request.Slug))
        {
            var slugExists = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.Slug == request.Slug, cancellationToken);

            if (slugExists)
                return Result<int>.Failure($"이미 사용 중인 슬러그입니다: {request.Slug}");
        }

        var product = new Product
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Price = request.Price,
            SalePrice = request.SalePrice,
            PriceType = request.PriceType,
            Specification = request.Specification,
            CategoryId = request.CategoryId,
            IsFeatured = request.IsFeatured,
            IsNew = request.IsNew,
            IsActive = true,
            CustomFieldsJson = request.CustomFieldsJson,
            CreatedBy = _currentUser.Username ?? "system"
        };

        if (request.Images?.Any() == true)
        {
            foreach (var img in request.Images)
            {
                product.Images.Add(new ProductImage
                {
                    Url = img.Url,
                    AltText = img.AltText,
                    SortOrder = img.SortOrder,
                    IsPrimary = img.IsPrimary,
                    CreatedBy = _currentUser.Username ?? "system"
                });
            }
        }

        if (request.Variants?.Any() == true)
        {
            foreach (var v in request.Variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Name = v.Name,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    IsActive = v.IsActive,
                    CreatedBy = _currentUser.Username ?? "system"
                });
            }
        }

        await _db.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(product.Id);
    }
}
