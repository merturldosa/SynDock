using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record CreateNotificationCommand(
    int UserId,
    string Type,
    string Title,
    string? Message,
    int? ReferenceId,
    string? ReferenceType
) : IRequest<Result<int>>;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationSender _sender;

    public CreateNotificationCommandHandler(IShopDbContext db, IUnitOfWork unitOfWork, INotificationSender sender)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<Result<int>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            IsRead = false,
            CreatedBy = "system"
        };

        await _db.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Push real-time notification via SignalR
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

        await _sender.SendToUser(request.UserId, dto, cancellationToken);

        // Send updated unread count
        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == request.UserId && !n.IsRead, cancellationToken);
        await _sender.SendUnreadCount(request.UserId, unreadCount, cancellationToken);

        return Result<int>.Success(notification.Id);
    }
}
