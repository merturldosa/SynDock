using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record ProductPerformanceDto(
    int ProductId, string ProductName, string? ImageUrl, string CategoryName,
    int ViewCount, int OrderCount, decimal Revenue,
    decimal ConversionRate, decimal AverageRating);

public record ProductPerformanceResultDto(
    IReadOnlyList<ProductPerformanceDto> Products, int TotalProducts);

public record GetProductPerformanceQuery(
    string? Sort = "revenue", int Page = 1, int PageSize = 20
) : IRequest<Result<ProductPerformanceResultDto>>;

public class GetProductPerformanceQueryHandler : IRequestHandler<GetProductPerformanceQuery, Result<ProductPerformanceResultDto>>
{
    private readonly IShopDbContext _db;

    public GetProductPerformanceQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ProductPerformanceResultDto>> Handle(GetProductPerformanceQuery request, CancellationToken cancellationToken)
    {
        var validStatuses = new[] { nameof(OrderStatus.Confirmed), nameof(OrderStatus.Processing), nameof(OrderStatus.Shipped), nameof(OrderStatus.Delivered) };

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                CategoryName = p.Category.Name,
                p.ViewCount,
                OrderCount = _db.OrderItems
                    .Where(oi => oi.ProductId == p.Id && validStatuses.Contains(oi.Order.Status))
                    .Sum(oi => oi.Quantity),
                Revenue = _db.OrderItems
                    .Where(oi => oi.ProductId == p.Id && validStatuses.Contains(oi.Order.Status))
                    .Sum(oi => (decimal?)oi.TotalPrice) ?? 0,
                AverageRating = _db.Reviews
                    .Where(r => r.ProductId == p.Id && r.IsVisible)
                    .Average(r => (double?)r.Rating) ?? 0
            })
            .ToListAsync(cancellationToken);

        var totalProducts = products.Count;

        var performanceList = products.Select(p => new ProductPerformanceDto(
            p.Id, p.Name, p.ImageUrl, p.CategoryName,
            p.ViewCount, p.OrderCount,
            p.Revenue,
            p.ViewCount > 0 ? Math.Round((decimal)p.OrderCount / p.ViewCount * 100, 2) : 0,
            (decimal)Math.Round(p.AverageRating, 1)
        ));

        performanceList = request.Sort?.ToLower() switch
        {
            "views" => performanceList.OrderByDescending(p => p.ViewCount),
            "orders" => performanceList.OrderByDescending(p => p.OrderCount),
            "conversion" => performanceList.OrderByDescending(p => p.ConversionRate),
            "rating" => performanceList.OrderByDescending(p => p.AverageRating),
            _ => performanceList.OrderByDescending(p => p.Revenue),
        };

        var pagedItems = performanceList
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result<ProductPerformanceResultDto>.Success(
            new ProductPerformanceResultDto(pagedItems, totalProducts));
    }
}
