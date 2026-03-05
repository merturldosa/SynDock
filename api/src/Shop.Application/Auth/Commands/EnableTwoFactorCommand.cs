using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record EnableTwoFactorCommand(int UserId) : IRequest<Result<TwoFactorSetupResponse>>;

public class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, Result<TwoFactorSetupResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public EnableTwoFactorCommandHandler(
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

    public async Task<Result<TwoFactorSetupResponse>> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return Result<TwoFactorSetupResponse>.Failure("User not found.");

        if (user.TwoFactorEnabled)
            return Result<TwoFactorSetupResponse>.Failure("Two-factor authentication is already enabled.");

        // Generate secret and backup codes
        var secret = _totpService.GenerateSecret();
        var backupCodes = _totpService.GenerateBackupCodes();
        var qrCodeUri = _totpService.GenerateQrCodeUri(secret, user.Email, "SynDock.Shop");

        // Store secret temporarily (not yet enabled until verified)
        user.TwoFactorSecret = secret;
        user.TwoFactorBackupCodes = System.Text.Json.JsonSerializer.Serialize(backupCodes);
        user.UpdatedBy = _currentUser.Username ?? "system";
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TwoFactorSetupResponse>.Success(
            new TwoFactorSetupResponse(qrCodeUri, secret, backupCodes));
    }
}
