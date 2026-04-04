using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record DashboardStatsDto(
    int TotalProducts,
    int TotalCategories,
    int TotalOrders,
    decimal TotalRevenue,
    int TotalUsers,
    IReadOnlyList<OrderStatusCount> OrdersByStatus,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    IReadOnlyList<TopProductDto> TopProducts,
    int LowStockCount,
    int TodayOrders,
    decimal TodayRevenue,
    IReadOnlyList<CategorySalesDto> CategorySales);

public record OrderStatusCount(string Status, int Count);
public record RecentOrderDto(int Id, string OrderNumber, string Status, decimal TotalAmount, DateTime CreatedAt);
public record TopProductDto(int ProductId, string ProductName, string? ImageUrl, int OrderCount, decimal TotalSales);
public record CategorySalesDto(string CategoryName, decimal TotalSales, int OrderCount);

public record GetDashboardStatsQuery : IRequest<Result<DashboardStatsDto>>;

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IShopDbContext _db;

    public GetDashboardStatsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DashboardStatsDto>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var totalProducts = await _db.Products.CountAsync(cancellationToken);
        var totalCategories = await _db.Categories.CountAsync(cancellationToken);
        var totalOrders = await _db.Orders.CountAsync(cancellationToken);
        var totalRevenue = await _db.Orders
            .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.Status != nameof(OrderStatus.Refunded))
            .Select(o => (decimal?)o.TotalAmount)
            .SumAsync(cancellationToken) ?? 0;
        var totalUsers = await _db.Users.CountAsync(cancellationToken);

        // Orders by status
        var ordersByStatus = await _db.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new OrderStatusCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        // Recent 5 orders
        var recentOrders = await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto(o.Id, o.OrderNumber, o.Status, o.TotalAmount, o.CreatedAt))
            .ToListAsync(cancellationToken);

        // Top 5 products by order count
        var topProducts = await _db.OrderItems
            .AsNoTracking()
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                OrderCount = g.Count(),
                TotalSales = g.Sum(oi => oi.TotalPrice)
            })
            .OrderByDescending(x => x.OrderCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topProductIds = topProducts.Select(tp => tp.ProductId).ToList();
        var productImages = await _db.ProductImages
            .AsNoTracking()
            .Where(pi => topProductIds.Contains(pi.ProductId) && pi.IsPrimary)
            .ToDictionaryAsync(pi => pi.ProductId, pi => pi.Url, cancellationToken);

        var topProductDtos = topProducts.Select(tp => new TopProductDto(
            tp.ProductId,
            tp.ProductName,
            productImages.GetValueOrDefault(tp.ProductId),
            tp.OrderCount,
            tp.TotalSales
        )).ToList();

        // Low stock count (threshold: 10)
        var lowStockCount = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive && v.Stock <= 10)
            .CountAsync(cancellationToken);

        // Today's orders and revenue
        var todayStart = DateTime.UtcNow.Date;
        var todayOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= todayStart)
            .CountAsync(cancellationToken);
        var todayRevenue = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= todayStart && o.Status != nameof(OrderStatus.Cancelled) && o.Status != nameof(OrderStatus.Refunded))
            .Select(o => (decimal?)o.TotalAmount)
            .SumAsync(cancellationToken) ?? 0;

        // Category sales (top 5) — try DB-side GroupBy first, fall back to in-memory for InMemory DB
        var cancelledStatuses = new[] { nameof(OrderStatus.Cancelled), nameof(OrderStatus.Refunded) };
        List<CategorySalesDto> categorySales;
        try
        {
            categorySales = await _db.OrderItems
                .AsNoTracking()
                .Where(oi => !cancelledStatuses.Contains(oi.Order.Status))
                .GroupBy(oi => oi.Product.Category.Name ?? "기타")
                .Select(g => new CategorySalesDto(
                    g.Key,
                    g.Sum(oi => oi.TotalPrice),
                    g.Select(oi => oi.OrderId).Distinct().Count()))
                .OrderByDescending(c => c.TotalSales)
                .Take(5)
                .ToListAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Fallback for InMemory DB which doesn't support GroupBy with navigation properties
            var allOrderItems = await _db.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .ToListAsync(cancellationToken);

            categorySales = allOrderItems
                .Where(oi => !cancelledStatuses.Contains(oi.Order.Status))
                .GroupBy(oi => oi.Product.Category?.Name ?? "기타")
                .Select(g => new CategorySalesDto(
                    g.Key,
                    g.Sum(oi => oi.TotalPrice),
                    g.Select(oi => oi.OrderId).Distinct().Count()))
                .OrderByDescending(c => c.TotalSales)
                .Take(5)
                .ToList();
        }

        return Result<DashboardStatsDto>.Success(new DashboardStatsDto(
            totalProducts, totalCategories, totalOrders, totalRevenue, totalUsers,
            ordersByStatus, recentOrders, topProductDtos,
            lowStockCount, todayOrders, todayRevenue, categorySales));
    }
}
