namespace Shop.Application.Common.Interfaces;

public interface IDriverNotifier
{
    Task NotifyNewDeliveryOffer(int tenantId, int zoneId, object offer, CancellationToken ct = default);
    Task NotifyDriverDirectly(int driverId, string eventName, object data, CancellationToken ct = default);
    Task BroadcastDriverLocation(int assignmentId, double lat, double lng, CancellationToken ct = default);
    Task NotifyDeliveryStatusChanged(int assignmentId, string status, CancellationToken ct = default);
}
