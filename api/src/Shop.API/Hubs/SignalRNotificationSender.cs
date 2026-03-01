using Microsoft.AspNetCore.SignalR;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Hubs;

public class SignalRNotificationSender : INotificationSender
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationSender(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUser(int userId, object notification, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(userId.ToString())
            .SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendUnreadCount(int userId, int count, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group(userId.ToString())
            .SendAsync("UpdateUnreadCount", count, ct);
    }
}
