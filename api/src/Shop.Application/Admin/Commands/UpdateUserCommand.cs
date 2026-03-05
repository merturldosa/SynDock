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
            return Result<bool>.Failure("Authentication required.");

        // Cannot change own role
        if (_currentUser.UserId.Value == request.UserId)
            return Result<bool>.Failure("Cannot change your own role.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        // Only PlatformAdmin can modify PlatformAdmin users
        var currentUserEntity = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user.Role == "PlatformAdmin" && currentUserEntity?.Role != "PlatformAdmin")
            return Result<bool>.Failure("Only PlatformAdmin can modify PlatformAdmin users.");

        var validRoles = new[] { "Member", "TenantAdmin", "Admin", "PlatformAdmin" };
        if (!validRoles.Contains(request.Role))
            return Result<bool>.Failure("Invalid role.");

        // Only PlatformAdmin can assign PlatformAdmin role
        if (request.Role == "PlatformAdmin" && currentUserEntity?.Role != "PlatformAdmin")
            return Result<bool>.Failure("Only PlatformAdmin can assign PlatformAdmin role.");

        // Only PlatformAdmin/Admin can assign TenantAdmin role
        if (request.Role == "TenantAdmin" && currentUserEntity?.Role is not ("PlatformAdmin" or "Admin"))
            return Result<bool>.Failure("Only Admin or above can assign TenantAdmin role.");

        // Only Admin/PlatformAdmin can assign Admin role
        if (request.Role == "Admin" && currentUserEntity?.Role is not ("PlatformAdmin" or "Admin"))
            return Result<bool>.Failure("Only Admin or above can assign Admin role.");

        // TenantAdmin can only assign Member role (cannot escalate to TenantAdmin/Admin/PlatformAdmin)
        if (currentUserEntity?.Role == "TenantAdmin" && request.Role != "Member")
            return Result<bool>.Failure("TenantAdmin can only assign Member role.");

        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedBy = _currentUser.Username;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
