using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Follows.Commands;

public record ToggleFollowCommand(int TargetUserId) : IRequest<Result<bool>>;

public class ToggleFollowCommandHandler : IRequestHandler<ToggleFollowCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleFollowCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ToggleFollowCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        if (_currentUser.UserId.Value == request.TargetUserId)
            return Result<bool>.Failure("Cannot follow yourself.");

        var targetExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.TargetUserId && u.IsActive, cancellationToken);

        if (!targetExists)
            return Result<bool>.Failure("User not found.");

        var existing = await _db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == _currentUser.UserId.Value
                && f.FollowingId == request.TargetUserId, cancellationToken);

        if (existing != null)
        {
            _db.Follows.Remove(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(false); // unfollowed
        }

        await _db.Follows.AddAsync(new Follow
        {
            FollowerId = _currentUser.UserId.Value,
            FollowingId = request.TargetUserId,
            CreatedBy = _currentUser.Username ?? "system"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true); // followed
    }
}
