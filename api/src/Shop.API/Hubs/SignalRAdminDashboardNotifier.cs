using Microsoft.AspNetCore.SignalR;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Hubs;

public class SignalRAdminDashboardNotifier : IAdminDashboardNotifier
{
    private readonly IHubContext<AdminHub> _hubContext;

    public SignalRAdminDashboardNotifier(IHubContext<AdminHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrder(int tenantId, string orderNumber, decimal totalAmount, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"admins-{tenantId}")
            .SendAsync("NewOrder", new { orderNumber, totalAmount, timestamp = DateTime.UtcNow }, ct);
    }

    public async Task NotifyOrderStatusChanged(int tenantId, string orderNumber, string newStatus, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"admins-{tenantId}")
            .SendAsync("OrderStatusChanged", new { orderNumber, newStatus, timestamp = DateTime.UtcNow }, ct);
    }
}
