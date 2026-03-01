namespace Shop.Application.Common.Interfaces;

public interface INotificationSender
{
    Task SendToUser(int userId, object notification, CancellationToken ct = default);
    Task SendUnreadCount(int userId, int count, CancellationToken ct = default);
}
