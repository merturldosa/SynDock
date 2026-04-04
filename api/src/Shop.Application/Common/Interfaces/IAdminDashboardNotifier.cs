namespace Shop.Application.Common.Interfaces;

public interface IAdminDashboardNotifier
{
    Task NotifyNewOrder(int tenantId, string orderNumber, decimal totalAmount, CancellationToken ct = default);
    Task NotifyOrderStatusChanged(int tenantId, string orderNumber, string newStatus, CancellationToken ct = default);
    Task NotifyMesSyncCompleted(int tenantId, int syncedCount, int failedCount, CancellationToken ct = default);
    Task NotifyAutoReorderTriggered(int tenantId, string orderNumber, int itemCount, int totalQuantity, CancellationToken ct = default);
    Task NotifyDeliveryAssigned(int tenantId, string orderNumber, string driverName, CancellationToken ct = default);
    Task NotifyDeliveryCompleted(int tenantId, string orderNumber, CancellationToken ct = default);
    Task NotifyDeliveryFailed(int tenantId, string orderNumber, string reason, CancellationToken ct = default);
}
