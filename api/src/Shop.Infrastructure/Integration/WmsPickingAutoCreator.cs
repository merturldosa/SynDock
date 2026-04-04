using MediatR;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;

namespace Shop.Infrastructure.Integration;

public class WmsPickingAutoCreator : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IWmsService _wms;
    private readonly ILogger<WmsPickingAutoCreator> _logger;

    public WmsPickingAutoCreator(IWmsService wms, ILogger<WmsPickingAutoCreator> logger)
    {
        _wms = wms;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        try
        {
            var picking = await _wms.CreatePickingOrderAsync(notification.TenantId, notification.OrderId, "system-auto", ct);
            _logger.LogInformation("Auto-created picking order {PickingNumber} for order {OrderNumber}", picking.PickingNumber, notification.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create picking for order {OrderNumber}", notification.OrderNumber);
        }
    }
}
