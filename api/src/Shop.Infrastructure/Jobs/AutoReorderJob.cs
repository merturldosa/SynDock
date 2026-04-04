using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class AutoReorderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<AutoReorderJob> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;

    public AutoReorderJob(IServiceProvider services, IConfiguration configuration, ILogger<AutoReorderJob> logger)
    {
        _services = services;
        _logger = logger;
        var mesMode = configuration["Mes:Enabled"]?.ToLower();
        _enabled = mesMode == "true" || mesMode == "demo";
        var minutes = configuration.GetValue<int>("Mes:AutoReorderIntervalMinutes", 30);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Auto-reorder job is disabled (MES not enabled)");
            return;
        }

        // Initial delay to let the app start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCreateOrders(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Auto-reorder job failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckAndCreateOrders(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var mesClient = scope.ServiceProvider.GetRequiredService<IMesClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMesProductMapper>();
        var notifier = scope.ServiceProvider.GetRequiredService<IAdminDashboardNotifier>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Get all tenants with enabled auto-reorder rules
        var tenantIds = await db.AutoReorderRules
            .IgnoreQueryFilters()
            .Where(r => r.IsEnabled)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                await ProcessTenantAutoReorder(db, mesClient, mapper, notifier, configuration, tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-reorder failed for tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task ProcessTenantAutoReorder(
        ShopDbContext db,
        IMesClient mesClient,
        IMesProductMapper mapper,
        IAdminDashboardNotifier notifier,
        IConfiguration configuration,
        int tenantId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Get enabled rules for this tenant
        var rules = await db.AutoReorderRules
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && r.IsEnabled)
            .ToListAsync(ct);

        if (rules.Count == 0) return;

        var productIds = rules.Select(r => r.ProductId).ToList();

        // Get current stock levels
        var stockMap = await db.ProductVariants
            .IgnoreQueryFilters()
            .Where(v => v.TenantId == tenantId && productIds.Contains(v.ProductId) && v.IsActive)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(v => v.Stock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, ct);

        // Find products that need reordering
        var itemsToOrder = new List<(AutoReorderRule Rule, int CurrentStock)>();

        foreach (var rule in rules)
        {
            var currentStock = stockMap.GetValueOrDefault(rule.ProductId, 0);

            // Skip if stock is above threshold
            if (currentStock > rule.ReorderThreshold)
                continue;

            // Skip if within minimum interval
            if (rule.LastTriggeredAt.HasValue &&
                (now - rule.LastTriggeredAt.Value).TotalHours < rule.MinIntervalHours)
                continue;

            itemsToOrder.Add((rule, currentStock));
        }

        if (itemsToOrder.Count == 0) return;

        _logger.LogInformation("Auto-reorder: {Count} products below threshold for tenant {TenantId}",
            itemsToOrder.Count, tenantId);

        // Create Purchase Order
        var poNumber = $"PO-AUTO-{now:yyyyMMddHHmmss}-{tenantId}";
        var po = new PurchaseOrder
        {
            TenantId = tenantId,
            OrderNumber = poNumber,
            Status = "Created",
            TriggerType = "Auto",
            CreatedByUser = "System (AutoReorder)",
            Notes = $"자동 발주 - {itemsToOrder.Count}개 상품 재고 부족 감지"
        };

        var totalQty = 0;
        foreach (var (rule, currentStock) in itemsToOrder)
        {
            var orderQty = rule.ReorderQuantity;

            // Auto-calculate quantity if not specified
            if (orderQty <= 0)
            {
                if (rule.MaxStockLevel > 0)
                {
                    // Fill up to max stock level
                    orderQty = Math.Max(1, rule.MaxStockLevel - currentStock);
                }
                else
                {
                    // Default: order 3x threshold amount
                    orderQty = Math.Max(1, rule.ReorderThreshold * 3 - currentStock);
                }
            }

            var mesCode = await mapper.GetMesProductCodeAsync(rule.ProductId, ct);

            po.Items.Add(new PurchaseOrderItem
            {
                TenantId = tenantId,
                ProductId = rule.ProductId,
                ProductName = rule.ProductName,
                MesProductCode = mesCode,
                CurrentStock = currentStock,
                ReorderThreshold = rule.ReorderThreshold,
                OrderedQuantity = orderQty,
                Reason = $"재고 {currentStock}개 ≤ 임계값 {rule.ReorderThreshold}개"
            });

            totalQty += orderQty;
            rule.LastTriggeredAt = now;
        }

        po.TotalQuantity = totalQty;
        po.ItemCount = po.Items.Count;

        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Auto-reorder PO created: {OrderNumber}, {ItemCount} items, {TotalQty} total qty",
            poNumber, po.ItemCount, totalQty);

        // Auto-forward to MES if configured
        var shouldForward = itemsToOrder.Any(i => i.Rule.AutoForwardToMes);
        if (shouldForward)
        {
            try
            {
                var isAvailable = await mesClient.IsAvailableAsync(ct);
                if (isAvailable)
                {
                    var mesItems = po.Items
                        .Where(i => !string.IsNullOrEmpty(i.MesProductCode))
                        .ToList();

                    if (mesItems.Count > 0)
                    {
                        var customerId = configuration.GetValue<long>("Mes:CustomerId", 1);
                        var salesUserId = configuration.GetValue<long>("Mes:SalesUserId", 1);

                        var lineNo = 0;
                        var salesOrderLines = mesItems.Select(i => new MesSalesOrderLine(
                            LineNo: ++lineNo,
                            ProductId: i.ProductId,
                            OrderedQuantity: i.OrderedQuantity,
                            Unit: "EA",
                            UnitPrice: 0 // Purchase orders don't have a unit price from shop side
                        )).ToList();

                        var mesRequest = new MesSalesOrderRequest(
                            OrderNo: poNumber,
                            OrderDate: now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            CustomerId: customerId,
                            SalesUserId: salesUserId,
                            Items: salesOrderLines);

                        var result = await mesClient.CreateSalesOrderAsync(mesRequest, ct);

                        if (result.Success)
                        {
                            po.Status = "Forwarded";
                            po.MesOrderId = result.MesOrderId;
                            po.ForwardedAt = DateTime.UtcNow;
                            await db.SaveChangesAsync(ct);

                            _logger.LogInformation("Auto-reorder PO forwarded to MES: {OrderNumber} → {MesOrderId}",
                                poNumber, result.MesOrderId);
                        }
                        else
                        {
                            _logger.LogWarning("Auto-reorder MES forwarding failed: {Error}", result.ErrorMessage);
                            po.Notes += $"\n[MES 전송 실패] {result.ErrorMessage}";
                            await db.SaveChangesAsync(ct);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Auto-reorder: MES server unavailable, PO created but not forwarded");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-reorder MES forwarding error for PO {OrderNumber}", poNumber);
            }
        }

        // Send admin notification
        try
        {
            await notifier.NotifyAutoReorderTriggered(tenantId, poNumber, po.ItemCount, totalQty, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send auto-reorder notification");
        }

        // Create in-app notifications for TenantAdmin users
        try
        {
            var adminUsers = await db.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == tenantId && u.IsActive &&
                    (u.Role == "TenantAdmin" || u.Role == "Admin" || u.Role == "PlatformAdmin"))
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var userId in adminUsers)
            {
                db.Notifications.Add(new Notification
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Type = "System",
                    Title = $"자동 발주 생성: {poNumber}",
                    Message = $"{po.ItemCount}개 상품, 총 {totalQty}개 수량 자동 발주가 생성되었습니다." +
                              (po.Status == "Forwarded" ? " (MES 전송 완료)" : ""),
                    ReferenceId = po.Id,
                    ReferenceType = "PurchaseOrder"
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-reorder in-app notifications");
        }
    }
}
