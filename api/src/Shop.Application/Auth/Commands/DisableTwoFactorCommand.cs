using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record DisableTwoFactorCommand(int UserId, string Code) : IRequest<Result<bool>>;

public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DisableTwoFactorCommandHandler(
        IShopDbContext db,
        ITotpService totpService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _db = db;
        _totpService = totpService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        if (!user.TwoFactorEnabled)
            return Result<bool>.Failure("Two-factor authentication is not enabled.");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result<bool>.Failure("Two-factor authentication setup not found.");

        // Validate the TOTP code before disabling
        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<bool>.Failure("Invalid verification code.");

        // Disable 2FA and clear secrets
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorBackupCodes = null;
        user.UpdatedBy = _currentUser.Username ?? "system";
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
