namespace Shop.Application.Common.Interfaces;

public interface IWebPushService
{
    Task SendPushAsync(int userId, string title, string message, string? url = null, CancellationToken ct = default);
    Task SendPushToAllAsync(int tenantId, string title, string message, string? url = null, CancellationToken ct = default);
}
