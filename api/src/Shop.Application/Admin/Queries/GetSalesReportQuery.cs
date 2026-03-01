using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record GetSalesReportQuery(DateTime StartDate, DateTime EndDate) : IRequest<Result<string>>;

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, Result<string>>
{
    private readonly IShopDbContext _db;

    public GetSalesReportQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<string>> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= request.StartDate.Date
                && o.CreatedAt < request.EndDate.Date.AddDays(1)
                && o.Status != nameof(OrderStatus.Cancelled)
                && o.Status != nameof(OrderStatus.Refunded))
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync(cancellationToken);

        var dailyData = orders
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, OrderCount = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(d => d.Date)
            .ToList();

        // Fill missing days
        var allDays = new List<(DateTime Date, int OrderCount, decimal Revenue)>();
        for (var date = request.StartDate.Date; date <= request.EndDate.Date; date = date.AddDays(1))
        {
            var existing = dailyData.FirstOrDefault(d => d.Date == date);
            allDays.Add(existing != null
                ? (date, existing.OrderCount, existing.Revenue)
                : (date, 0, 0));
        }

        var sb = new StringBuilder();
        sb.AppendLine("날짜,주문수,매출,평균주문금액");

        foreach (var day in allDays)
        {
            var avg = day.OrderCount > 0 ? day.Revenue / day.OrderCount : 0;
            sb.AppendLine($"{day.Date:yyyy-MM-dd},{day.OrderCount},{day.Revenue},{avg:F0}");
        }

        return Result<string>.Success(sb.ToString());
    }
}
