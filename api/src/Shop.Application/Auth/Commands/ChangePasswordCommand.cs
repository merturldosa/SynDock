using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result<bool>>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<bool>.Failure("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedBy = _currentUser.Username;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
