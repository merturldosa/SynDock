using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record CustomerSegmentDto(string Segment, int Count, decimal TotalSpent);
public record SpendTierDto(string Tier, int Count, decimal TotalSpent);
public record TopCustomerDto(int UserId, string Name, string Email, int OrderCount, decimal TotalSpent, DateTime LastOrderAt);

public record CustomerAnalyticsDto(
    int TotalCustomers,
    int NewCustomers30Days,
    int ReturningCustomers,
    IReadOnlyList<CustomerSegmentDto> Segments,
    IReadOnlyList<SpendTierDto> SpendTiers,
    IReadOnlyList<TopCustomerDto> TopCustomers);

public record GetCustomerAnalyticsQuery : IRequest<Result<CustomerAnalyticsDto>>;

public class GetCustomerAnalyticsQueryHandler : IRequestHandler<GetCustomerAnalyticsQuery, Result<CustomerAnalyticsDto>>
{
    private readonly IShopDbContext _db;

    public GetCustomerAnalyticsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CustomerAnalyticsDto>> Handle(GetCustomerAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var validStatuses = new[] { nameof(OrderStatus.Confirmed), nameof(OrderStatus.Processing), nameof(OrderStatus.Shipped), nameof(OrderStatus.Delivered) };
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var customerData = await _db.Orders
            .AsNoTracking()
            .Where(o => validStatuses.Contains(o.Status))
            .GroupBy(o => new { o.UserId, o.User.Name, o.User.Email })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.Name,
                g.Key.Email,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
                FirstOrderAt = g.Min(o => o.CreatedAt),
                LastOrderAt = g.Max(o => o.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var totalCustomers = customerData.Count;
        var newCustomers = customerData.Count(c => c.FirstOrderAt >= thirtyDaysAgo);
        var returningCustomers = customerData.Count(c => c.OrderCount >= 2);

        // Segments: New, Returning, VIP
        var segments = new List<CustomerSegmentDto>
        {
            new("신규", customerData.Count(c => c.FirstOrderAt >= thirtyDaysAgo),
                customerData.Where(c => c.FirstOrderAt >= thirtyDaysAgo).Sum(c => c.TotalSpent)),
            new("재구매", customerData.Count(c => c.OrderCount >= 2 && c.OrderCount < 5 && c.TotalSpent < 500000),
                customerData.Where(c => c.OrderCount >= 2 && c.OrderCount < 5 && c.TotalSpent < 500000).Sum(c => c.TotalSpent)),
            new("VIP", customerData.Count(c => c.OrderCount >= 5 || c.TotalSpent >= 500000),
                customerData.Where(c => c.OrderCount >= 5 || c.TotalSpent >= 500000).Sum(c => c.TotalSpent)),
            new("일반", customerData.Count(c => c.FirstOrderAt < thirtyDaysAgo && c.OrderCount < 2),
                customerData.Where(c => c.FirstOrderAt < thirtyDaysAgo && c.OrderCount < 2).Sum(c => c.TotalSpent)),
        };

        // Spend Tiers
        var spendTiers = new List<SpendTierDto>
        {
            new("0~5만원", customerData.Count(c => c.TotalSpent < 50000),
                customerData.Where(c => c.TotalSpent < 50000).Sum(c => c.TotalSpent)),
            new("5만~20만원", customerData.Count(c => c.TotalSpent >= 50000 && c.TotalSpent < 200000),
                customerData.Where(c => c.TotalSpent >= 50000 && c.TotalSpent < 200000).Sum(c => c.TotalSpent)),
            new("20만~50만원", customerData.Count(c => c.TotalSpent >= 200000 && c.TotalSpent < 500000),
                customerData.Where(c => c.TotalSpent >= 200000 && c.TotalSpent < 500000).Sum(c => c.TotalSpent)),
            new("50만원+", customerData.Count(c => c.TotalSpent >= 500000),
                customerData.Where(c => c.TotalSpent >= 500000).Sum(c => c.TotalSpent)),
        };

        // Top 10 Customers
        var topCustomers = customerData
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .Select(c => new TopCustomerDto(c.UserId, c.Name, c.Email, c.OrderCount, c.TotalSpent, c.LastOrderAt))
            .ToList();

        return Result<CustomerAnalyticsDto>.Success(new CustomerAnalyticsDto(
            totalCustomers, newCustomers, returningCustomers, segments, spendTiers, topCustomers));
    }
}
