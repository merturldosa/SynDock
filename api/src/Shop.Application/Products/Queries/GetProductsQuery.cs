using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Products.Queries;

public record GetProductsQuery(
    string? Category,
    string? Search,
    string? Sort,
    int Page = 1,
    int PageSize = 20,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    decimal? MinRating = null,
    bool? IsFeatured = null,
    bool? IsNew = null
) : IRequest<PagedList<ProductListDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedList<ProductListDto>>
{
    private readonly IShopDbContext _db;

    public GetProductsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedList<ProductListDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        // Category filter
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = int.TryParse(request.Category, out var categoryId)
                ? query.Where(p => p.CategoryId == categoryId)
                : query.Where(p => p.Category.Slug == request.Category);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        // Price range
        if (request.MinPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.Price) >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.Price) <= request.MaxPrice.Value);

        // Featured / New
        if (request.IsFeatured == true)
            query = query.Where(p => p.IsFeatured);
        if (request.IsNew == true)
            query = query.Where(p => p.IsNew);

        // Rating filter
        if (request.MinRating.HasValue)
            query = query.Where(p => _db.Reviews
                .Where(r => r.ProductId == p.Id && r.IsVisible)
                .Average(r => (double?)r.Rating) >= (double)request.MinRating.Value);

        // Sort (accept both hyphen and underscore)
        query = request.Sort?.ToLower() switch
        {
            "price-asc" or "price_asc" => query.OrderBy(p => p.SalePrice ?? p.Price),
            "price-desc" or "price_desc" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
            "name" or "name-asc" or "name_asc" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "popular" => query.OrderByDescending(p => p.ViewCount),
            _ => query.OrderBy(p => p.SortOrder).ThenByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListDto(
                p.Id, p.Name, p.Slug, p.Price, p.SalePrice,
                p.PriceType, p.Specification, p.CategoryId, p.Category.Name,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                p.IsFeatured, p.IsNew, p.ViewCount,
                p.Variants.Where(v => v.IsActive).Sum(v => v.Stock)))
            .ToListAsync(cancellationToken);

        return new PagedList<ProductListDto>(items, totalCount, request.Page, request.PageSize);
    }
}
