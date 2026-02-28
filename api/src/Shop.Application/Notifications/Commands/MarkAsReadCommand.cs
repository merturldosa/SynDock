using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Commands;

public record MarkAsReadCommand(int Id) : IRequest<Result<bool>>;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAsReadCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == _currentUser.UserId.Value, cancellationToken);

        if (notification is null)
            return Result<bool>.Failure("알림을 찾을 수 없습니다.");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
