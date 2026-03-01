using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Admin.Commands;

public record UpdateUserCommand(int UserId, string Role, bool IsActive) : IRequest<Result<bool>>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        // Cannot change own role
        if (_currentUser.UserId.Value == request.UserId)
            return Result<bool>.Failure("본인의 역할은 변경할 수 없습니다.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("사용자를 찾을 수 없습니다.");

        // Only PlatformAdmin can modify PlatformAdmin users
        var currentUserEntity = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user.Role == "PlatformAdmin" && currentUserEntity?.Role != "PlatformAdmin")
            return Result<bool>.Failure("PlatformAdmin 변경은 PlatformAdmin만 가능합니다.");

        var validRoles = new[] { "Member", "Admin", "PlatformAdmin" };
        if (!validRoles.Contains(request.Role))
            return Result<bool>.Failure("유효하지 않은 역할입니다.");

        // Only PlatformAdmin can assign PlatformAdmin role
        if (request.Role == "PlatformAdmin" && currentUserEntity?.Role != "PlatformAdmin")
            return Result<bool>.Failure("PlatformAdmin 역할 부여는 PlatformAdmin만 가능합니다.");

        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedBy = _currentUser.Username;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
