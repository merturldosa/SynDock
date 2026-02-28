using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record DailySalesDto(DateTime Date, int OrderCount, decimal Revenue);

public record SalesAnalyticsDto(
    IReadOnlyList<DailySalesDto> DailySales,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue);

public record GetSalesAnalyticsQuery(int Days = 30) : IRequest<Result<SalesAnalyticsDto>>;

public class GetSalesAnalyticsQueryHandler : IRequestHandler<GetSalesAnalyticsQuery, Result<SalesAnalyticsDto>>
{
    private readonly IShopDbContext _db;

    public GetSalesAnalyticsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SalesAnalyticsDto>> Handle(GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-request.Days);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate
                && o.Status != nameof(OrderStatus.Cancelled)
                && o.Status != nameof(OrderStatus.Refunded))
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(cancellationToken);

        var dailySales = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new DailySalesDto(g.Key, g.Count(), g.Sum(o => o.TotalAmount)))
            .OrderBy(d => d.Date)
            .ToList();

        // Fill in missing days with zero
        var allDays = new List<DailySalesDto>();
        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var existing = dailySales.FirstOrDefault(d => d.Date == date);
            allDays.Add(existing ?? new DailySalesDto(date, 0, 0));
        }

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        return Result<SalesAnalyticsDto>.Success(new SalesAnalyticsDto(
            allDays, totalRevenue, totalOrders, averageOrderValue));
    }
}
