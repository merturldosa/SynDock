using Microsoft.AspNetCore.SignalR;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Hubs;

public class SignalRDriverNotifier : IDriverNotifier
{
    private readonly IHubContext<DriverHub> _hubContext;

    public SignalRDriverNotifier(IHubContext<DriverHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewDeliveryOffer(int tenantId, int zoneId, object offer, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"zone-{tenantId}-{zoneId}")
            .SendAsync("NewDeliveryOffer", offer, ct);
    }

    public async Task NotifyDriverDirectly(int driverId, string eventName, object data, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"driver-{driverId}")
            .SendAsync(eventName, data, ct);
    }

    public async Task BroadcastDriverLocation(int assignmentId, double lat, double lng, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"delivery-{assignmentId}")
            .SendAsync("DriverLocationUpdate", new { latitude = lat, longitude = lng, timestamp = DateTime.UtcNow }, ct);
    }

    public async Task NotifyDeliveryStatusChanged(int assignmentId, string status, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"delivery-{assignmentId}")
            .SendAsync("DeliveryStatusChanged", new { assignmentId, status, timestamp = DateTime.UtcNow }, ct);
    }
}
