using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record DeleteNotificationCommand(int Id) : IRequest<Result<bool>>;

public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteNotificationCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _currentUser.UserId.Value, cancellationToken);

        if (notification is null)
            return Result<bool>.Failure("Notification not found.");

        _db.Notifications.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
