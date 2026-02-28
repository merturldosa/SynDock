using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Products.Queries;

public record GetProductByIdQuery(int Id) : IRequest<ProductDetailDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto?>
{
    private readonly IShopDbContext _db;

    public GetProductByIdQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants.Where(v => v.IsActive).OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive, cancellationToken);

        if (product == null) return null;

        return new ProductDetailDto(
            product.Id, product.Name, product.Slug, product.Description,
            product.Price, product.SalePrice, product.PriceType, product.Specification,
            product.CategoryId, product.Category.Name, product.IsFeatured, product.IsNew, product.ViewCount,
            product.CustomFieldsJson,
            product.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.SortOrder, i.IsPrimary)).ToList(),
            product.Variants.Select(v => new ProductVariantDto(v.Id, v.Name, v.Sku, v.Price, v.Stock, v.IsActive)).ToList()
        );
    }
}
