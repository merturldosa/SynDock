using Microsoft.EntityFrameworkCore;
using Shop.Application.Common;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Infrastructure.Services;

public class PlanEnforcer : IPlanEnforcer
{
    private readonly IShopDbContext _db;

    public PlanEnforcer(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> CanCreateProduct(int tenantId, CancellationToken ct)
    {
        var (usage, limits) = await GetUsageAndLimits(tenantId, ct);
        if (usage.ProductCount >= limits.MaxProducts)
            return Result<bool>.Failure($"Product limit ({limits.MaxProducts}) exceeded. Plan upgrade required.");
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CanRegisterUser(int tenantId, CancellationToken ct)
    {
        var (usage, limits) = await GetUsageAndLimits(tenantId, ct);
        if (usage.UserCount >= limits.MaxUsers)
            return Result<bool>.Failure($"User limit ({limits.MaxUsers}) exceeded. Plan upgrade required.");
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CanPlaceOrder(int tenantId, CancellationToken ct)
    {
        var (usage, limits) = await GetUsageAndLimits(tenantId, ct);
        var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
        var monthlyOrders = usage.CurrentPeriod == currentPeriod ? usage.MonthlyOrderCount : 0;
        if (monthlyOrders >= limits.MaxMonthlyOrders)
            return Result<bool>.Failure($"Monthly order limit ({limits.MaxMonthlyOrders}) exceeded. Plan upgrade required.");
        return Result<bool>.Success(true);
    }

    public async Task EnsureUsageTracked(int tenantId, CancellationToken ct)
    {
        var exists = await _db.TenantUsages
            .AnyAsync(u => u.TenantId == tenantId, ct);

        if (!exists)
        {
            // Calculate current usage from actual data
            var productCount = await _db.Products.IgnoreQueryFilters()
                .CountAsync(p => p.TenantId == tenantId, ct);
            var userCount = await _db.Users.IgnoreQueryFilters()
                .CountAsync(u => u.TenantId == tenantId, ct);
            var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthlyOrders = await _db.Orders.IgnoreQueryFilters()
                .CountAsync(o => o.TenantId == tenantId && o.CreatedAt >= startOfMonth, ct);

            var usage = new TenantUsage
            {
                TenantId = tenantId,
                ProductCount = productCount,
                UserCount = userCount,
                MonthlyOrderCount = monthlyOrders,
                CurrentPeriod = currentPeriod,
                CreatedBy = "System"
            };
            await _db.TenantUsages.AddAsync(usage, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task IncrementProductCount(int tenantId, int delta, CancellationToken ct)
    {
        var usage = await GetOrCreateUsage(tenantId, ct);
        usage.ProductCount = Math.Max(0, usage.ProductCount + delta);
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementUserCount(int tenantId, int delta, CancellationToken ct)
    {
        var usage = await GetOrCreateUsage(tenantId, ct);
        usage.UserCount = Math.Max(0, usage.UserCount + delta);
        await _db.SaveChangesAsync(ct);
    }

    public async Task IncrementOrderCount(int tenantId, int delta, CancellationToken ct)
    {
        var usage = await GetOrCreateUsage(tenantId, ct);
        var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
        if (usage.CurrentPeriod != currentPeriod)
        {
            usage.CurrentPeriod = currentPeriod;
            usage.MonthlyOrderCount = 0;
        }
        usage.MonthlyOrderCount = Math.Max(0, usage.MonthlyOrderCount + delta);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(TenantUsage usage, PlanLimits.Limits limits)> GetUsageAndLimits(int tenantId, CancellationToken ct)
    {
        await EnsureUsageTracked(tenantId, ct);
        var usage = await _db.TenantUsages.FirstAsync(u => u.TenantId == tenantId, ct);
        var plan = await _db.TenantPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);
        var limits = PlanLimits.GetLimits(plan?.PlanType ?? "Free");
        return (usage, limits);
    }

    private async Task<TenantUsage> GetOrCreateUsage(int tenantId, CancellationToken ct)
    {
        await EnsureUsageTracked(tenantId, ct);
        return await _db.TenantUsages.FirstAsync(u => u.TenantId == tenantId, ct);
    }
}
