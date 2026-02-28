using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record MarkAllAsReadCommand : IRequest<Result<int>>;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllAsReadCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;

        var unreadNotifications = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(unreadNotifications.Count);
    }
}
