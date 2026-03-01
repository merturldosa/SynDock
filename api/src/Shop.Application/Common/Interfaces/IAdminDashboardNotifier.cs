namespace Shop.Application.Common.Interfaces;

public interface IAdminDashboardNotifier
{
    Task NotifyNewOrder(int tenantId, string orderNumber, decimal totalAmount, CancellationToken ct = default);
    Task NotifyOrderStatusChanged(int tenantId, string orderNumber, string newStatus, CancellationToken ct = default);
}
