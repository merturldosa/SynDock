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
    decimal AverageOrderValue,
    decimal? PreviousPeriodRevenue = null,
    int? PreviousPeriodOrders = null,
    decimal? RevenueChangePercent = null,
    decimal? OrdersChangePercent = null);

public record GetSalesAnalyticsQuery(
    int? Days = 30,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool IncludeComparison = false
) : IRequest<Result<SalesAnalyticsDto>>;

public class GetSalesAnalyticsQueryHandler : IRequestHandler<GetSalesAnalyticsQuery, Result<SalesAnalyticsDto>>
{
    private readonly IShopDbContext _db;

    public GetSalesAnalyticsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SalesAnalyticsDto>> Handle(GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        DateTime startDate, endDate;

        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            startDate = request.StartDate.Value.Date;
            endDate = request.EndDate.Value.Date;
        }
        else
        {
            var days = request.Days ?? 30;
            endDate = DateTime.UtcNow.Date;
            startDate = endDate.AddDays(-days);
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate
                && o.CreatedAt < endDate.AddDays(1)
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
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var existing = dailySales.FirstOrDefault(d => d.Date == date);
            allDays.Add(existing ?? new DailySalesDto(date, 0, 0));
        }

        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var totalOrders = orders.Count;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        decimal? prevRevenue = null, revenueChange = null, ordersChange = null;
        int? prevOrders = null;

        if (request.IncludeComparison)
        {
            var periodDays = (endDate - startDate).Days + 1;
            var prevStart = startDate.AddDays(-periodDays);
            var prevEnd = startDate.AddDays(-1);

            var prevData = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= prevStart
                    && o.CreatedAt < prevEnd.AddDays(1)
                    && o.Status != nameof(OrderStatus.Cancelled)
                    && o.Status != nameof(OrderStatus.Refunded))
                .Select(o => new { o.TotalAmount })
                .ToListAsync(cancellationToken);

            prevRevenue = prevData.Sum(o => o.TotalAmount);
            prevOrders = prevData.Count;
            revenueChange = prevRevenue > 0 ? Math.Round((totalRevenue - prevRevenue.Value) / prevRevenue.Value * 100, 1) : null;
            ordersChange = prevOrders > 0 ? Math.Round((decimal)(totalOrders - prevOrders.Value) / prevOrders.Value * 100, 1) : null;
        }

        return Result<SalesAnalyticsDto>.Success(new SalesAnalyticsDto(
            allDays, totalRevenue, totalOrders, averageOrderValue,
            prevRevenue, prevOrders, revenueChange, ordersChange));
    }
}
