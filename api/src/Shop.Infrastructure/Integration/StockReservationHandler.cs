using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class StockReservationHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IShopDbContext _db;
    private readonly IWmsService _wms;
    private readonly ILogger<StockReservationHandler> _logger;

    public StockReservationHandler(IShopDbContext db, IWmsService wms, ILogger<StockReservationHandler> logger)
    {
        _db = db;
        _wms = wms;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        try
        {
            var items = await _db.OrderItems.AsNoTracking()
                .Where(oi => oi.OrderId == notification.OrderId)
                .ToListAsync(ct);

            foreach (var item in items)
            {
                await _wms.RecordMovementAsync(
                    notification.TenantId,
                    item.ProductId,
                    item.VariantId,
                    movementType: "Outbound",
                    quantity: item.Quantity,
                    fromLocationId: null,
                    toLocationId: null,
                    orderId: notification.OrderId,
                    reason: $"Reserved for order {notification.OrderNumber}",
                    createdBy: "system-auto",
                    ct: ct);
            }

            _logger.LogInformation("Stock reserved for order {OrderNumber}: {Count} items", notification.OrderNumber, items.Count);

            // Check for low stock / out of stock after reservation → trigger AI reorder
            foreach (var item in items)
            {
                var variant = await _db.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductId == item.ProductId && v.TenantId == notification.TenantId, ct);

                if (variant != null && variant.Stock <= 10)
                {
                    var urgency = variant.Stock <= 0 ? "CRITICAL" : "LOW";
                    _logger.LogWarning("⚠️ {Urgency} STOCK: Product {ProductName} (ID:{ProductId}) stock={Stock} — AI reorder triggered",
                        urgency, item.ProductName, item.ProductId, variant.Stock);

                    // Auto-create reorder rule if none exists
                    var hasRule = await _db.AutoReorderRules
                        .AnyAsync(r => r.TenantId == notification.TenantId && r.ProductId == item.ProductId, ct);

                    if (!hasRule)
                    {
                        _db.AutoReorderRules.Add(new Domain.Entities.AutoReorderRule
                        {
                            TenantId = notification.TenantId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            ReorderThreshold = 10,
                            ReorderQuantity = 50,
                            IsEnabled = true,
                            AutoForwardToMes = true,
                            CreatedBy = "AI-StockAlert"
                        });
                        _logger.LogInformation("AI auto-created reorder rule for {Product}: threshold=10, qty=50", item.ProductName);
                    }

                    // Auto-create production plan suggestion for critical stock
                    if (variant.Stock <= 0)
                    {
                        var hasSuggestion = await _db.ProductionPlanSuggestions
                            .AnyAsync(s => s.TenantId == notification.TenantId && s.ProductId == item.ProductId && s.Status == "Pending", ct);

                        if (!hasSuggestion)
                        {
                            _db.ProductionPlanSuggestions.Add(new Domain.Entities.ProductionPlanSuggestion
                            {
                                TenantId = notification.TenantId,
                                ProductId = item.ProductId,
                                ProductName = item.ProductName,
                                CurrentStock = variant.Stock,
                                SuggestedQuantity = 100,
                                Urgency = "Critical",
                                Status = "Pending",
                                AiReason = $"재고 소진: {item.ProductName} 현재 재고 {variant.Stock}. 즉시 생산 필요.",
                                ConfidenceScore = 0.95,
                                CreatedBy = "AI-StockAlert"
                            });
                            _logger.LogWarning("🏭 AI auto-created CRITICAL production plan for {Product}", item.ProductName);
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve stock for order {OrderNumber}", notification.OrderNumber);
        }
    }
}
