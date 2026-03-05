using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record VerifyTwoFactorLoginCommand(string TwoFactorToken, string Code) : IRequest<Result<AuthResponse>>;

public class VerifyTwoFactorLoginCommandHandler : IRequestHandler<VerifyTwoFactorLoginCommand, Result<AuthResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITotpService _totpService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyTwoFactorLoginCommandHandler(
        IShopDbContext db,
        ITokenService tokenService,
        ITotpService totpService,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _tokenService = tokenService;
        _totpService = totpService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        // Validate the temporary 2FA token
        var userId = _tokenService.ValidateTwoFactorToken(request.TwoFactorToken);
        if (userId is null)
            return Result<AuthResponse>.Failure("Two-factor authentication token is invalid or expired.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive, cancellationToken);

        if (user is null)
            return Result<AuthResponse>.Failure("User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result<AuthResponse>.Failure("Two-factor authentication is not configured.");

        // Try TOTP code first
        var isValidTotp = _totpService.ValidateCode(user.TwoFactorSecret, request.Code);

        // If TOTP fails, try backup codes
        if (!isValidTotp)
        {
            var backupUsed = TryUseBackupCode(user, request.Code);
            if (!backupUsed)
                return Result<AuthResponse>.Failure("Invalid verification code.");
        }

        // Complete login - same as normal login flow
        user.LastLoginAt = DateTime.UtcNow;

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            TenantId = _tenantContext.TenantId,
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = user.Username
        };
        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, _tenantContext.Tenant!);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            refreshTokenValue,
            new UserDto(user.Id, user.Username, user.Email, user.Name, user.Phone, user.Role, user.CustomFieldsJson)
        ));
    }

    private static bool TryUseBackupCode(User user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFactorBackupCodes))
            return false;

        try
        {
            var backupCodes = JsonSerializer.Deserialize<List<string>>(user.TwoFactorBackupCodes);
            if (backupCodes is null || !backupCodes.Contains(code))
                return false;

            // Remove used backup code
            backupCodes.Remove(code);
            user.TwoFactorBackupCodes = JsonSerializer.Serialize(backupCodes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
