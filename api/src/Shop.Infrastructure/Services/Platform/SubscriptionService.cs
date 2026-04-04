using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IShopDbContext _db;
    public SubscriptionService(IShopDbContext db) => _db = db;

    public async Task<TenantPlan?> GetCurrentPlanAsync(int tenantId, CancellationToken ct = default)
        => await _db.TenantPlans.AsNoTracking().FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);

    public async Task ChangePlanAsync(int tenantId, string newPlanType, string updatedBy, CancellationToken ct = default)
    {
        var plan = await _db.TenantPlans.FirstOrDefaultAsync(p => p.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Plan not found");

        plan.PlanType = newPlanType;
        plan.MonthlyPrice = newPlanType switch
        {
            "Free" => 0, "Starter" => 50000, "Pro" => 150000, "Enterprise" => 500000, _ => plan.MonthlyPrice
        };
        plan.BillingStatus = newPlanType == "Free" ? "Free" : "Active";
        plan.NextBillingAt = newPlanType == "Free" ? null : DateTime.UtcNow.AddMonths(1);
        plan.UpdatedBy = updatedBy;
        plan.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetUsageSummaryAsync(int tenantId, CancellationToken ct = default)
    {
        var productCount = await _db.Products.CountAsync(p => p.TenantId == tenantId, ct);
        var userCount = await _db.Users.CountAsync(u => u.TenantId == tenantId, ct);
        var orderCount = await _db.Orders.CountAsync(o => o.TenantId == tenantId, ct);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthlyOrders = await _db.Orders.CountAsync(o => o.TenantId == tenantId && o.CreatedAt >= monthStart, ct);
        var monthlyRevenue = await _db.Orders.Where(o => o.TenantId == tenantId && o.CreatedAt >= monthStart)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

        return new
        {
            tenantId, productCount, userCount, totalOrders = orderCount,
            monthlyOrders, monthlyRevenue, period = monthStart.ToString("yyyy-MM")
        };
    }

    public async Task<List<Invoice>> GetInvoicesAsync(int tenantId, CancellationToken ct = default)
        => await _db.Invoices.AsNoTracking().Where(i => i.TenantId == tenantId).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

    public async Task ProcessMonthlyBillingAsync(CancellationToken ct = default)
    {
        var dueDate = DateTime.UtcNow;
        var plans = await _db.TenantPlans
            .Where(p => p.BillingStatus == "Active" && p.NextBillingAt != null && p.NextBillingAt <= dueDate)
            .ToListAsync(ct);

        foreach (var plan in plans)
        {
            // Create invoice
            var invoice = new Invoice
            {
                TenantId = plan.TenantId,
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{plan.TenantId:D4}",
                BillingPeriod = DateTime.UtcNow.ToString("yyyy-MM"),
                Amount = plan.MonthlyPrice,
                Status = "Issued",
                CreatedBy = "system"
            };
            _db.Invoices.Add(invoice);
            plan.NextBillingAt = DateTime.UtcNow.AddMonths(1);
        }

        await _db.SaveChangesAsync(ct);
    }
}
