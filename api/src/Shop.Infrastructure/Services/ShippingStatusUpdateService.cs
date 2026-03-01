using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Infrastructure.Data;
using Shop.Infrastructure.Shipping;

namespace Shop.Infrastructure.Services;

public class ShippingStatusUpdateService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShippingStatusUpdateService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    public ShippingStatusUpdateService(
        IServiceScopeFactory scopeFactory,
        ILogger<ShippingStatusUpdateService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ShippingStatusUpdateService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckShippedOrders(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking shipped orders");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckShippedOrders(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var tracker = scope.ServiceProvider.GetRequiredService<IShippingTracker>();

        // Get all shipped orders with tracking info (bypass tenant filter)
        var shippedOrders = await db.Orders
            .IgnoreQueryFilters()
            .Where(o => o.Status == nameof(OrderStatus.Shipped))
            .Select(o => new
            {
                o.Id,
                o.UserId,
                o.TenantId,
                o.OrderNumber,
                TrackingHistory = o.Histories
                    .Where(h => h.TrackingNumber != null)
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefault()
            })
            .Where(o => o.TrackingHistory != null)
            .ToListAsync(ct);

        _logger.LogInformation("Checking {Count} shipped orders for delivery status", shippedOrders.Count);

        foreach (var order in shippedOrders)
        {
            if (order.TrackingHistory is null || string.IsNullOrEmpty(order.TrackingHistory.TrackingNumber))
                continue;

            try
            {
                var carrier = order.TrackingHistory.TrackingCarrier ?? "CJ대한통운";
                var result = await tracker.GetTrackingInfo(carrier, order.TrackingHistory.TrackingNumber, ct);

                if (result.IsSuccess && DeliveryTrackerService.IsDelivered(result.CurrentStatus))
                {
                    // Update order to Delivered
                    var orderEntity = await db.Orders.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(o => o.Id == order.Id, ct);

                    if (orderEntity is not null && orderEntity.Status == nameof(OrderStatus.Shipped))
                    {
                        orderEntity.Status = nameof(OrderStatus.Delivered);
                        orderEntity.UpdatedBy = "system";
                        orderEntity.UpdatedAt = DateTime.UtcNow;

                        await db.OrderHistories.AddAsync(new OrderHistory
                        {
                            TenantId = order.TenantId,
                            OrderId = order.Id,
                            Status = nameof(OrderStatus.Delivered),
                            Note = "배송이 완료되었습니다.",
                            TrackingNumber = order.TrackingHistory.TrackingNumber,
                            TrackingCarrier = order.TrackingHistory.TrackingCarrier,
                            CreatedBy = "system"
                        }, ct);

                        await db.Notifications.AddAsync(new Notification
                        {
                            TenantId = order.TenantId,
                            UserId = order.UserId,
                            Type = nameof(NotificationType.Order),
                            Title = "배송 완료",
                            Message = $"주문 {order.OrderNumber}이(가) 배달 완료되었습니다.",
                            ReferenceId = order.Id,
                            ReferenceType = "Order",
                            CreatedBy = "system"
                        }, ct);

                        await db.SaveChangesAsync(ct);
                        _logger.LogInformation("Order {OrderNumber} marked as Delivered", order.OrderNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check tracking for order {OrderId}", order.Id);
            }
        }
    }
}
