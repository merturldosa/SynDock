using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class MisController : ControllerBase
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MisController(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Unified MIS Dashboard - All modules in one view</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1);
        var lastMonth = thisMonth.AddMonths(-1);
        var thisWeek = now.AddDays(-7);

        // Shop metrics
        var totalOrders = await _db.Orders.CountAsync(ct);
        var monthlyOrders = await _db.Orders.CountAsync(o => o.CreatedAt >= thisMonth, ct);
        var monthlyRevenue = await _db.Orders.Where(o => o.CreatedAt >= thisMonth && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
        var lastMonthRevenue = await _db.Orders.Where(o => o.CreatedAt >= lastMonth && o.CreatedAt < thisMonth && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
        var revenueGrowth = lastMonthRevenue > 0 ? (monthlyRevenue - lastMonthRevenue) / lastMonthRevenue * 100 : 0;

        // WMS metrics
        var totalSkus = await _db.Products.CountAsync(p => p.IsActive, ct);
        var lowStockCount = await _db.ProductVariants.CountAsync(v => v.Stock <= 10 && v.Stock > 0, ct);
        var outOfStockCount = await _db.ProductVariants.CountAsync(v => v.Stock <= 0, ct);
        var pendingPicking = await _db.PickingOrders.CountAsync(p => p.Status == "Pending", ct);
        var pendingPacking = await _db.PackingSlips.CountAsync(p => p.Status == "Pending", ct);

        // CRM metrics
        var totalCustomers = await _db.Users.CountAsync(u => u.Role == "Member", ct);
        var newCustomersThisMonth = await _db.Users.CountAsync(u => u.Role == "Member" && u.CreatedAt >= thisMonth, ct);
        var openTickets = await _db.CsTickets.CountAsync(t => t.Status == "Open" || t.Status == "InProgress", ct);
        var avgSatisfaction = await _db.CsTickets.Where(t => t.SatisfactionRating != null)
            .AverageAsync(t => (double?)t.SatisfactionRating, ct) ?? 0;

        // ERP metrics
        var monthlyExpenses = await _db.AccountEntries
            .Where(e => e.EntryDate >= thisMonth && e.Status == "Posted" && e.EntryType == "Debit" && e.Account.AccountType == "Expense")
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var totalEmployees = await _db.Employees.CountAsync(e => e.Status == "Active", ct);

        // SCM metrics
        var activeSuppliers = await _db.Suppliers.CountAsync(s => s.Status == "Active", ct);
        var openPurchaseOrders = await _db.ProcurementOrders.CountAsync(po => po.Status != "Delivered" && po.Status != "Cancelled", ct);
        var overduePOs = await _db.ProcurementOrders.CountAsync(po =>
            po.ExpectedDeliveryDate != null && po.ExpectedDeliveryDate < now
            && po.Status != "Delivered" && po.Status != "Cancelled", ct);

        // AI Automation metrics
        var aiReorderRules = await _db.AutoReorderRules.CountAsync(r => r.IsEnabled, ct);
        var aiProductionSuggestions = await _db.ProductionPlanSuggestions
            .CountAsync(s => s.Status == "Pending" && s.CreatedAt > thisWeek, ct);

        return Ok(new
        {
            generatedAt = now,
            shop = new
            {
                totalOrders, monthlyOrders, monthlyRevenue, lastMonthRevenue,
                revenueGrowth = Math.Round(revenueGrowth, 1),
                avgOrderValue = monthlyOrders > 0 ? Math.Round(monthlyRevenue / monthlyOrders) : 0
            },
            wms = new
            {
                totalSkus, lowStockCount, outOfStockCount,
                pendingPicking, pendingPacking,
                stockHealthPercent = totalSkus > 0 ? Math.Round((double)(totalSkus - lowStockCount - outOfStockCount) / totalSkus * 100, 1) : 100
            },
            crm = new
            {
                totalCustomers, newCustomersThisMonth, openTickets,
                avgSatisfaction = Math.Round(avgSatisfaction, 1),
                customerGrowthRate = totalCustomers > 0 ? Math.Round((double)newCustomersThisMonth / totalCustomers * 100, 1) : 0
            },
            erp = new
            {
                monthlyRevenue, monthlyExpenses,
                netIncome = monthlyRevenue - monthlyExpenses,
                profitMargin = monthlyRevenue > 0 ? Math.Round((double)(monthlyRevenue - monthlyExpenses) / (double)monthlyRevenue * 100, 1) : 0,
                totalEmployees
            },
            scm = new
            {
                activeSuppliers, openPurchaseOrders, overduePOs,
                supplyChainHealth = openPurchaseOrders > 0 ? Math.Round((double)(openPurchaseOrders - overduePOs) / openPurchaseOrders * 100, 1) : 100
            },
            ai = new
            {
                activeReorderRules = aiReorderRules,
                pendingProductionSuggestions = aiProductionSuggestions,
                automationLevel = "Active"
            }
        });
    }

    /// <summary>AI Event Flow - Recent automation events across all modules</summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetEventFlow([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var events = new List<object>();

        // Recent orders (Shop)
        var recentOrders = await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt).Take(10)
            .Select(o => new { Type = "Order", o.OrderNumber, o.TotalAmount, o.Status, o.CreatedAt })
            .ToListAsync(ct);
        events.AddRange(recentOrders.Select(o => new
        {
            module = "Shop", type = "Order", detail = $"Order {o.OrderNumber}: \u20a9{o.TotalAmount:N0}",
            status = o.Status, timestamp = o.CreatedAt
        }));

        // Recent picking orders (WMS)
        var recentPicking = await _db.PickingOrders.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt).Take(10)
            .Select(p => new { p.PickingNumber, p.Status, p.TotalItems, p.CreatedAt, p.CreatedBy })
            .ToListAsync(ct);
        events.AddRange(recentPicking.Select(p => new
        {
            module = "WMS", type = "Picking",
            detail = $"Pick {p.PickingNumber}: {p.TotalItems} items",
            status = p.Status, timestamp = p.CreatedAt,
            isAuto = p.CreatedBy == "system-auto" || p.CreatedBy == "AI-Orchestrator"
        }));

        // Recent GL entries (ERP)
        var recentEntries = await _db.AccountEntries.AsNoTracking()
            .Where(e => e.CreatedBy == "AI-Automator" || e.CreatedBy == "system-auto")
            .OrderByDescending(e => e.CreatedAt).Take(10)
            .Select(e => new { e.EntryNumber, e.Amount, e.EntryType, e.Description, e.CreatedAt })
            .ToListAsync(ct);
        events.AddRange(recentEntries.Select(e => new
        {
            module = "ERP", type = "JournalEntry",
            detail = $"{e.EntryNumber}: {e.EntryType} \u20a9{e.Amount:N0}",
            status = "Posted", timestamp = e.CreatedAt, isAuto = true
        }));

        // Recent CS tickets (CRM)
        var recentTickets = await _db.CsTickets.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt).Take(10)
            .Select(t => new { t.TicketNumber, t.Subject, t.Status, t.Priority, t.CreatedAt, t.CreatedBy })
            .ToListAsync(ct);
        events.AddRange(recentTickets.Select(t => new
        {
            module = "CRM", type = "Ticket",
            detail = $"{t.TicketNumber}: {t.Subject}",
            status = t.Status, timestamp = t.CreatedAt,
            isAuto = t.CreatedBy == "system-auto"
        }));

        // Recent procurement orders (SCM)
        var recentPOs = await _db.ProcurementOrders.AsNoTracking()
            .OrderByDescending(po => po.CreatedAt).Take(10)
            .Select(po => new { po.OrderNumber, po.TotalAmount, po.Status, po.CreatedAt, po.CreatedBy })
            .ToListAsync(ct);
        events.AddRange(recentPOs.Select(po => new
        {
            module = "SCM", type = "ProcurementOrder",
            detail = $"{po.OrderNumber}: \u20a9{po.TotalAmount:N0}",
            status = po.Status, timestamp = po.CreatedAt,
            isAuto = po.CreatedBy == "AI-Orchestrator"
        }));

        // Recent production suggestions (MES)
        var recentSuggestions = await _db.ProductionPlanSuggestions.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt).Take(10)
            .Select(s => new { s.ProductName, s.SuggestedQuantity, s.Urgency, s.Status, s.CreatedAt, s.CreatedBy })
            .ToListAsync(ct);
        events.AddRange(recentSuggestions.Select(s => new
        {
            module = "MES", type = "ProductionPlan",
            detail = $"{s.ProductName}: {s.SuggestedQuantity} units ({s.Urgency})",
            status = s.Status, timestamp = s.CreatedAt,
            isAuto = s.CreatedBy == "AI-Orchestrator"
        }));

        // Sort all events by timestamp and limit
        var sorted = events
            .OrderByDescending(e => ((dynamic)e).timestamp)
            .Take(limit)
            .ToList();

        var aiAutoCount = sorted.Count(e =>
        {
            try { return ((dynamic)e).isAuto == true; } catch { return false; }
        });

        return Ok(new
        {
            events = sorted,
            totalEvents = sorted.Count,
            aiAutoEvents = aiAutoCount,
            aiAutomationRate = sorted.Count > 0 ? Math.Round((double)aiAutoCount / sorted.Count * 100, 1) : 0
        });
    }

    /// <summary>Cross-tenant user activity tracking</summary>
    [HttpGet("user-activity")]
    public async Task<IActionResult> GetUserActivity([FromQuery] int? userId, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var query = _db.CustomerJourneyEvents.IgnoreQueryFilters()
            .Where(e => e.TenantId == 0) // Platform-level events
            .AsNoTracking();

        if (userId.HasValue)
            query = query.Where(e => e.UserId == userId.Value);

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new
            {
                e.UserId,
                e.EventType,
                e.EventDetail,
                e.Channel,
                e.CreatedAt
            })
            .ToListAsync(ct);

        var uniqueUsers = events.Select(e => e.UserId).Distinct().Count();
        var eventCounts = events.GroupBy(e => e.EventType).Select(g => new { type = g.Key, count = g.Count() });

        return Ok(new { events, uniqueUsers, eventCounts, total = events.Count });
    }

    /// <summary>Revenue trend for charts</summary>
    [HttpGet("revenue-trend")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int months = 6, CancellationToken ct = default)
    {
        var result = new List<object>();
        for (int i = months - 1; i >= 0; i--)
        {
            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
            var end = start.AddMonths(1);
            var revenue = await _db.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt < end && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
            var orders = await _db.Orders.CountAsync(o => o.CreatedAt >= start && o.CreatedAt < end, ct);
            result.Add(new { month = start.ToString("yyyy-MM"), revenue, orders });
        }
        return Ok(result);
    }
}
