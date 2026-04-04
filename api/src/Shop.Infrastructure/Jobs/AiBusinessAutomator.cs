using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;
using System.Text.Json;

namespace Shop.Infrastructure.Jobs;

/// <summary>
/// AI Business Automator - Handles the "back office" side of the autonomous chain.
///
/// Flow: Sales Data → ERP Auto-Accounting → CRM AI Customer Care → AI Marketing → Consumer Loop
///
/// Runs every 2 hours.
/// </summary>
public class AiBusinessAutomator : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AiBusinessAutomator> _logger;

    public AiBusinessAutomator(IServiceProvider services, ILogger<AiBusinessAutomator> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMinutes(3), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("=== AI Business Automator: Starting cycle ===");
                await RunAutomationCycleAsync(ct);
                _logger.LogInformation("=== AI Business Automator: Cycle complete ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Business Automator failed");
            }

            await Task.Delay(TimeSpan.FromHours(2), ct);
        }
    }

    private async Task RunAutomationCycleAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var crm = scope.ServiceProvider.GetRequiredService<ICrmService>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var tenants = await db.Tenants.Where(t => t.IsActive).Select(t => new { t.Id, t.Slug }).ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                await AutoAccountingAsync(db, tenant.Id, tenant.Slug, ct);
                await AutoCrmCareAsync(db, crm, tenant.Id, tenant.Slug, ct);
                await AutoMarketingAsync(db, email, tenant.Id, tenant.Slug, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Business automator failed for tenant {Slug}", tenant.Slug);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI-4: ERP Auto-Accounting - Auto-create journal entries
    // ═══════════════════════════════════════════════════════════════════
    private async Task AutoAccountingAsync(ShopDbContext db, int tenantId, string slug, CancellationToken ct)
    {
        // Find orders that don't have GL entries yet (confirmed but not journaled)
        var unjournaledOrders = await db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId
                && o.Status == "Confirmed"
                && o.CreatedAt > DateTime.UtcNow.AddDays(-7))
            .Select(o => new { o.Id, o.OrderNumber, o.TotalAmount, o.ShippingFee })
            .ToListAsync(ct);

        // Check which already have entries
        var existingRefs = await db.AccountEntries
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.ReferenceType == "Order")
            .Select(e => e.ReferenceId)
            .ToListAsync(ct);

        var newOrders = unjournaledOrders.Where(o => !existingRefs.Contains(o.Id)).ToList();

        if (newOrders.Count > 0)
        {
            var arAccount = await db.ChartOfAccounts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == "1200", ct);
            var revenueAccount = await db.ChartOfAccounts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == "4100", ct);

            if (arAccount != null && revenueAccount != null)
            {
                foreach (var order in newOrders)
                {
                    var entryNumber = $"JE-AUTO-{DateTime.UtcNow:yyyyMMdd}-{order.Id}";

                    // Debit: Accounts Receivable
                    db.AccountEntries.Add(new AccountEntry
                    {
                        TenantId = tenantId,
                        EntryNumber = $"{entryNumber}-D",
                        ChartOfAccountId = arAccount.Id,
                        EntryDate = DateTime.UtcNow,
                        EntryType = "Debit",
                        Amount = order.TotalAmount,
                        Description = $"Auto: Sales Order {order.OrderNumber}",
                        ReferenceType = "Order",
                        ReferenceId = order.Id,
                        Status = "Posted",
                        CreatedBy = "AI-Automator"
                    });

                    // Credit: Sales Revenue
                    db.AccountEntries.Add(new AccountEntry
                    {
                        TenantId = tenantId,
                        EntryNumber = $"{entryNumber}-C",
                        ChartOfAccountId = revenueAccount.Id,
                        EntryDate = DateTime.UtcNow,
                        EntryType = "Credit",
                        Amount = order.TotalAmount,
                        Description = $"Auto: Sales Order {order.OrderNumber}",
                        ReferenceType = "Order",
                        ReferenceId = order.Id,
                        Status = "Posted",
                        CreatedBy = "AI-Automator"
                    });
                }
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("[{Slug}] AI auto-created {Count} GL entries for orders", slug, newOrders.Count * 2);
            }
        }

        // Auto-create payroll entries from approved payrolls
        var unpaidPayrolls = await db.Payrolls
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.Status == "Paid" && p.PaidAt > DateTime.UtcNow.AddDays(-30))
            .ToListAsync(ct);

        var payrollRefs = await db.AccountEntries
            .IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.ReferenceType == "Payroll")
            .Select(e => e.ReferenceId)
            .ToListAsync(ct);

        var newPayrolls = unpaidPayrolls.Where(p => !payrollRefs.Contains(p.Id)).ToList();
        if (newPayrolls.Count > 0)
        {
            var expenseAccount = await db.ChartOfAccounts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == "5200", ct); // Labor cost
            var cashAccount = await db.ChartOfAccounts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == "1100", ct); // Cash

            if (expenseAccount != null && cashAccount != null)
            {
                foreach (var payroll in newPayrolls)
                {
                    db.AccountEntries.Add(new AccountEntry
                    {
                        TenantId = tenantId,
                        EntryNumber = $"JE-PAY-{payroll.PayPeriod}-{payroll.Id}-D",
                        ChartOfAccountId = expenseAccount.Id,
                        EntryDate = payroll.PaidAt ?? DateTime.UtcNow,
                        EntryType = "Debit",
                        Amount = payroll.NetPay,
                        Description = $"Auto: Payroll {payroll.PayPeriod}",
                        ReferenceType = "Payroll",
                        ReferenceId = payroll.Id,
                        Status = "Posted",
                        CreatedBy = "AI-Automator"
                    });
                    db.AccountEntries.Add(new AccountEntry
                    {
                        TenantId = tenantId,
                        EntryNumber = $"JE-PAY-{payroll.PayPeriod}-{payroll.Id}-C",
                        ChartOfAccountId = cashAccount.Id,
                        EntryDate = payroll.PaidAt ?? DateTime.UtcNow,
                        EntryType = "Credit",
                        Amount = payroll.NetPay,
                        Description = $"Auto: Payroll {payroll.PayPeriod}",
                        ReferenceType = "Payroll",
                        ReferenceId = payroll.Id,
                        Status = "Posted",
                        CreatedBy = "AI-Automator"
                    });
                }
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("[{Slug}] AI auto-created {Count} payroll GL entries", slug, newPayrolls.Count * 2);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI-5: CRM Auto Customer Care - AI-driven customer engagement
    // ═══════════════════════════════════════════════════════════════════
    private async Task AutoCrmCareAsync(ShopDbContext db, ICrmService crm, int tenantId, string slug, CancellationToken ct)
    {
        // Detect churning customers (no order in 60+ days, had orders before)
        var churnDate = DateTime.UtcNow.AddDays(-60);
        var activeDate = DateTime.UtcNow.AddDays(-180);

        var churnCandidates = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Role == "Member" && u.IsActive)
            .Where(u => db.Orders.IgnoreQueryFilters().Any(o => o.UserId == u.Id && o.CreatedAt > activeDate))
            .Where(u => !db.Orders.IgnoreQueryFilters().Any(o => o.UserId == u.Id && o.CreatedAt > churnDate))
            .Select(u => new { u.Id, u.Name, u.Email })
            .Take(50)
            .ToListAsync(ct);

        foreach (var customer in churnCandidates)
        {
            // Check if we already tracked this churn
            var alreadyTracked = await db.CustomerJourneyEvents
                .IgnoreQueryFilters()
                .AnyAsync(e => e.UserId == customer.Id && e.EventType == "ChurnRisk"
                    && e.CreatedAt > DateTime.UtcNow.AddDays(-7), ct);

            if (!alreadyTracked)
            {
                await crm.TrackEventAsync(tenantId, customer.Id, "ChurnRisk",
                    $"No purchase in 60+ days (last active within 180 days)",
                    null, "User", null, "AI-System", null, ct);

                _logger.LogInformation("[{Slug}] AI detected churn risk: {Customer} ({Email})",
                    slug, customer.Name, customer.Email);
            }
        }

        // Detect VIP candidates (high spending, frequent orders)
        var vipThreshold = DateTime.UtcNow.AddDays(-90);
        var vipCandidates = await db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.CreatedAt > vipThreshold && o.Status != "Cancelled")
            .GroupBy(o => o.UserId)
            .Where(g => g.Count() >= 5 || g.Sum(o => o.TotalAmount) >= 500000)
            .Select(g => g.Key)
            .ToListAsync(ct);

        foreach (var userId in vipCandidates)
        {
            // Auto-assign VIP tag
            var vipTag = await db.CustomerTags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Name == "VIP", ct);

            if (vipTag == null)
            {
                vipTag = new CustomerTag { TenantId = tenantId, Name = "VIP", Color = "#F59E0B", Description = "AI-detected VIP customer", CreatedBy = "AI-Automator" };
                db.CustomerTags.Add(vipTag);
                await db.SaveChangesAsync(ct);
            }

            var hasTag = await db.CustomerTagAssignments.IgnoreQueryFilters()
                .AnyAsync(a => a.UserId == userId && a.CustomerTagId == vipTag.Id, ct);

            if (!hasTag)
            {
                db.CustomerTagAssignments.Add(new CustomerTagAssignment
                {
                    TenantId = tenantId, UserId = userId, CustomerTagId = vipTag.Id, CreatedBy = "AI-Automator"
                });
                _logger.LogInformation("[{Slug}] AI auto-tagged user {UserId} as VIP", slug, userId);
            }
        }

        await db.SaveChangesAsync(ct);

        if (churnCandidates.Count > 0 || vipCandidates.Count > 0)
            _logger.LogInformation("[{Slug}] AI CRM: {Churn} churn risks, {Vip} VIP candidates detected",
                slug, churnCandidates.Count, vipCandidates.Count);
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI-6: Auto Marketing - AI-driven promotions and outreach
    // ═══════════════════════════════════════════════════════════════════
    private async Task AutoMarketingAsync(ShopDbContext db, IEmailService email, int tenantId, string slug, CancellationToken ct)
    {
        // Detect sales decline (compare this week vs last week)
        var thisWeekStart = DateTime.UtcNow.AddDays(-7);
        var lastWeekStart = DateTime.UtcNow.AddDays(-14);

        var thisWeekSales = await db.Orders.IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= thisWeekStart && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

        var lastWeekSales = await db.Orders.IgnoreQueryFilters()
            .Where(o => o.TenantId == tenantId && o.CreatedAt >= lastWeekStart && o.CreatedAt < thisWeekStart && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

        // If sales dropped more than 20%, trigger auto-promotion
        if (lastWeekSales > 0 && thisWeekSales < lastWeekSales * 0.8m)
        {
            var dropPercent = (1 - thisWeekSales / lastWeekSales) * 100;

            // Check if we already created a promo this week
            var recentPromo = await db.Coupons.IgnoreQueryFilters()
                .AnyAsync(c => c.TenantId == tenantId && c.CreatedAt > thisWeekStart && c.Code.StartsWith("AI-BOOST"), ct);

            if (!recentPromo)
            {
                // Auto-create boost coupon
                var coupon = new Coupon
                {
                    TenantId = tenantId,
                    Code = $"AI-BOOST-{DateTime.UtcNow:MMdd}",
                    Description = $"AI 자동 프로모션: 매출 {dropPercent:F0}% 감소 감지",
                    DiscountType = "Percent",
                    DiscountValue = 10, // 10% off
                    MinOrderAmount = 30000,
                    MaxUsageCount = 100,
                    CurrentUsageCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7),
                    IsActive = true,
                    CreatedBy = "AI-Automator"
                };
                db.Coupons.Add(coupon);
                await db.SaveChangesAsync(ct);

                _logger.LogWarning("[{Slug}] AI detected {Drop:F0}% sales decline. Auto-created boost coupon: {Code} (10% off, 7 days)",
                    slug, dropPercent, coupon.Code);

                // Auto-create email campaign for the coupon
                db.EmailCampaigns.Add(new EmailCampaign
                {
                    TenantId = tenantId,
                    Title = $"AI Boost Campaign - {DateTime.UtcNow:MM/dd}",
                    Content = $"<h2>10% 할인 쿠폰</h2><p>쿠폰 코드: <b>{coupon.Code}</b></p><p>3만원 이상 구매 시 사용 가능 (7일 한정)</p>",
                    Status = "Scheduled",
                    ScheduledAt = DateTime.UtcNow.AddHours(1),
                    CreatedBy = "AI-Automator"
                });
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("[{Slug}] AI auto-scheduled email campaign for boost coupon", slug);
            }
        }

        // Auto-create social post for best-selling products
        var bestSeller = await db.OrderItems.IgnoreQueryFilters()
            .Where(oi => oi.Order.TenantId == tenantId && oi.Order.CreatedAt > thisWeekStart)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .OrderByDescending(g => g.Sum(x => x.Quantity))
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, Qty = g.Sum(x => x.Quantity) })
            .FirstOrDefaultAsync(ct);

        if (bestSeller != null)
        {
            var recentPost = await db.SocialPosts.IgnoreQueryFilters()
                .AnyAsync(p => p.TenantId == tenantId && p.CreatedAt > DateTime.UtcNow.AddDays(-3), ct);

            if (!recentPost)
            {
                db.SocialPosts.Add(new SocialPost
                {
                    TenantId = tenantId,
                    Platform = "Instagram",
                    Caption = $"이번 주 베스트셀러! {bestSeller.ProductName} - {bestSeller.Qty}개 판매! #베스트셀러 #인기상품",
                    Status = "Pending",
                    ProductId = bestSeller.ProductId,
                    CreatedBy = "AI-Automator"
                });
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("[{Slug}] AI auto-scheduled social post for best-seller: {Product}", slug, bestSeller.ProductName);
            }
        }
    }
}
