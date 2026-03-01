using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record BroadcastNotificationCommand(string Title, string Message, string Type) : IRequest<Result<int>>;

public class BroadcastNotificationCommandHandler : IRequestHandler<BroadcastNotificationCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationSender _sender;

    public BroadcastNotificationCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork, INotificationSender sender)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<Result<int>> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
    {
        var activeUsers = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var notifications = activeUsers.Select(userId => new Notification
        {
            UserId = userId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            IsRead = false,
            CreatedBy = "admin"
        }).ToList();

        await _db.Notifications.AddRangeAsync(notifications, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Push real-time notifications
        foreach (var notification in notifications)
        {
            var dto = new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.IsRead,
                notification.ReadAt,
                notification.ReferenceId,
                notification.ReferenceType,
                notification.CreatedAt);

            await _sender.SendToUser(notification.UserId, dto, cancellationToken);

            var unreadCount = await _db.Notifications
                .CountAsync(n => n.UserId == notification.UserId && !n.IsRead, cancellationToken);
            await _sender.SendUnreadCount(notification.UserId, unreadCount, cancellationToken);
        }

        return Result<int>.Success(notifications.Count);
    }
}
