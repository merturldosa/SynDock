using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

/// <summary>
/// AI Supply Chain Orchestrator - The core of SynDock's autonomous business concept.
///
/// Flow: Consumer Purchase → AI Demand Forecast → WMS Auto-Reorder → MES Auto-Production → SCM Auto-Procurement
///
/// Runs every hour to analyze demand patterns and automatically trigger the supply chain.
/// </summary>
public class AiSupplyChainOrchestrator : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AiSupplyChainOrchestrator> _logger;

    public AiSupplyChainOrchestrator(IServiceProvider services, ILogger<AiSupplyChainOrchestrator> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), ct); // Wait for app startup

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("=== AI Supply Chain Orchestrator: Starting cycle ===");
                await RunOrchestrationCycleAsync(ct);
                _logger.LogInformation("=== AI Supply Chain Orchestrator: Cycle complete ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Supply Chain Orchestrator failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }

    private async Task RunOrchestrationCycleAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var forecast = scope.ServiceProvider.GetRequiredService<IDemandForecastService>();

        // Get all active tenants
        var tenants = await db.Tenants
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug })
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            try
            {
                await ProcessTenantAsync(db, forecast, tenant.Id, tenant.Slug, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestrator failed for tenant {Slug}", tenant.Slug);
            }
        }
    }

    private async Task ProcessTenantAsync(
        ShopDbContext db, IDemandForecastService forecast,
        int tenantId, string slug, CancellationToken ct)
    {
        _logger.LogInformation("[{Slug}] Processing tenant...", slug);

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: AI Demand Analysis - Analyze sales velocity
        // ═══════════════════════════════════════════════════════════════
        var products = await db.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        var lowStockProducts = new List<(int ProductId, string ProductName, int CurrentStock, double AvgDailySales, int DaysUntilStockout, int SuggestedQty)>();

        foreach (var product in products)
        {
            try
            {
                var result = await forecast.ForecastAsync(product.Id, 30, ct);
                if (result == null) continue;

                var variant = await db.ProductVariants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.ProductId == product.Id && v.TenantId == tenantId, ct);

                var currentStock = variant?.Stock ?? 0;
                var avgDaily = result.ForecastedDemand.Count > 0
                    ? result.ForecastedDemand.Average(d => (double)d.Quantity)
                    : 0;
                var daysUntilOut = avgDaily > 0 ? (int)(currentStock / avgDaily) : 999;

                // Flag products running low within 14 days
                if (daysUntilOut <= 14 && avgDaily > 0)
                {
                    var suggestedQty = (int)Math.Ceiling(avgDaily * 30) - currentStock; // 30-day cover
                    if (suggestedQty > 0)
                    {
                        lowStockProducts.Add((product.Id, product.Name, currentStock, avgDaily, daysUntilOut, suggestedQty));
                    }
                }
            }
            catch { /* Skip forecast errors for individual products */ }
        }

        if (lowStockProducts.Count == 0)
        {
            _logger.LogInformation("[{Slug}] All products healthy - no action needed", slug);
            return;
        }

        _logger.LogInformation("[{Slug}] {Count} products need attention", slug, lowStockProducts.Count);

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: WMS Auto-Reorder - Update reorder rules based on AI
        // ═══════════════════════════════════════════════════════════════
        foreach (var item in lowStockProducts)
        {
            var rule = await db.AutoReorderRules
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.ProductId == item.ProductId, ct);

            if (rule == null)
            {
                // Auto-create reorder rule based on AI forecast
                rule = new AutoReorderRule
                {
                    TenantId = tenantId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ReorderThreshold = (int)Math.Ceiling(item.AvgDailySales * 7), // 7-day safety stock
                    ReorderQuantity = item.SuggestedQty,
                    IsEnabled = true,
                    AutoForwardToMes = true,
                    CreatedBy = "AI-Orchestrator"
                };
                db.AutoReorderRules.Add(rule);
                _logger.LogInformation("[{Slug}] AI created reorder rule for {Product}: threshold={Threshold}, qty={Qty}",
                    slug, item.ProductName, rule.ReorderThreshold, rule.ReorderQuantity);
            }
            else
            {
                // AI adjusts existing rule based on latest forecast
                var newThreshold = (int)Math.Ceiling(item.AvgDailySales * 7);
                if (Math.Abs(rule.ReorderThreshold - newThreshold) > 2)
                {
                    rule.ReorderThreshold = newThreshold;
                    rule.ReorderQuantity = item.SuggestedQty;
                    rule.UpdatedBy = "AI-Orchestrator";
                    rule.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("[{Slug}] AI adjusted reorder rule for {Product}: threshold={Threshold}",
                        slug, item.ProductName, newThreshold);
                }
            }
        }
        await db.SaveChangesAsync(ct);

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: MES Production Auto-Suggest - Create production plans
        // ═══════════════════════════════════════════════════════════════
        var criticalProducts = lowStockProducts.Where(p => p.DaysUntilStockout <= 7).ToList();

        foreach (var item in criticalProducts)
        {
            var existingSuggestion = await db.ProductionPlanSuggestions
                .IgnoreQueryFilters()
                .AnyAsync(s => s.TenantId == tenantId && s.ProductId == item.ProductId && s.Status == "Pending", ct);

            if (!existingSuggestion)
            {
                var urgency = item.DaysUntilStockout <= 3 ? "Critical" : "High";

                db.ProductionPlanSuggestions.Add(new ProductionPlanSuggestion
                {
                    TenantId = tenantId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    CurrentStock = item.CurrentStock,
                    AverageDailySales = item.AvgDailySales,
                    EstimatedDaysUntilStockout = item.DaysUntilStockout,
                    SuggestedQuantity = item.SuggestedQty,
                    Urgency = urgency,
                    Status = urgency == "Critical" ? "Approved" : "Pending", // Auto-approve critical items
                    AiReason = $"AI Forecast: avg daily sales {item.AvgDailySales:F1} units, {item.DaysUntilStockout} days until stockout. Suggested production: {item.SuggestedQty} units for 30-day cover.",
                    ConfidenceScore = 0.85,
                    CreatedBy = "AI-Orchestrator"
                });

                _logger.LogInformation("[{Slug}] AI created {Urgency} production suggestion for {Product}: {Qty} units",
                    slug, urgency, item.ProductName, item.SuggestedQty);

                // Auto-approve critical: forward to MES immediately
                if (urgency == "Critical")
                {
                    _logger.LogWarning("[{Slug}] CRITICAL: Auto-forwarding {Product} to MES for immediate production",
                        slug, item.ProductName);
                }
            }
        }
        await db.SaveChangesAsync(ct);

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: SCM Auto-Procurement - Create purchase orders for suppliers
        // ═══════════════════════════════════════════════════════════════
        var supplierProducts = lowStockProducts.Where(p => p.DaysUntilStockout <= 10).ToList();

        if (supplierProducts.Count > 0)
        {
            // Find best supplier for auto-procurement
            var bestSupplier = await db.Suppliers
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.Status == "Active")
                .OrderByDescending(s => s.OnTimeDeliveryRate)
                .ThenBy(s => s.LeadTimeDays)
                .FirstOrDefaultAsync(ct);

            if (bestSupplier != null)
            {
                // Check if there's already a recent open PO
                var recentPo = await db.ProcurementOrders
                    .IgnoreQueryFilters()
                    .AnyAsync(po => po.TenantId == tenantId && po.SupplierId == bestSupplier.Id
                        && po.Status != "Delivered" && po.Status != "Cancelled"
                        && po.CreatedAt > DateTime.UtcNow.AddDays(-3), ct);

                if (!recentPo)
                {
                    var poNumber = $"PO-AI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
                    var po = new ProcurementOrder
                    {
                        TenantId = tenantId,
                        OrderNumber = poNumber,
                        SupplierId = bestSupplier.Id,
                        Status = "Submitted", // Auto-submit
                        ExpectedDeliveryDate = DateTime.UtcNow.AddDays(bestSupplier.LeadTimeDays),
                        SubmittedAt = DateTime.UtcNow,
                        Notes = $"AI Auto-Procurement: {supplierProducts.Count} products need restocking based on demand forecast",
                        CreatedBy = "AI-Orchestrator"
                    };

                    decimal totalAmount = 0;
                    foreach (var item in supplierProducts)
                    {
                        var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == item.ProductId, ct);
                        var unitPrice = product?.Price ?? 0;
                        var lineTotal = unitPrice * item.SuggestedQty * 0.6m; // Assume 60% of retail as procurement cost

                        po.Items.Add(new ProcurementOrderItem
                        {
                            TenantId = tenantId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.SuggestedQty,
                            UnitPrice = unitPrice * 0.6m,
                            TotalPrice = lineTotal,
                            CreatedBy = "AI-Orchestrator"
                        });
                        totalAmount += lineTotal;
                    }

                    po.TotalAmount = totalAmount;
                    db.ProcurementOrders.Add(po);
                    await db.SaveChangesAsync(ct);

                    _logger.LogInformation("[{Slug}] AI auto-created procurement order {PO} to {Supplier}: {Items} items, total ₩{Amount:N0}",
                        slug, poNumber, bestSupplier.Name, supplierProducts.Count, totalAmount);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // STEP 5: Log AI orchestration event for MIS tracking
        // ═══════════════════════════════════════════════════════════════
        _logger.LogInformation(
            "[{Slug}] AI Orchestration Summary: {LowStock} low-stock, {Critical} critical, {Reorder} reorder rules updated, {Procurement} procurement candidates",
            slug, lowStockProducts.Count, criticalProducts.Count, lowStockProducts.Count, supplierProducts.Count);
    }
}
