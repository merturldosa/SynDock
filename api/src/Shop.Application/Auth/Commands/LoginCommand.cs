using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(IShopDbContext db, ITokenService tokenService, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // First try tenant-scoped user lookup (global query filter applies)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        // SSO fallback: check for platform user (TenantId=0) who can access any tenant
        if (user == null)
        {
            user = await _db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == 0 && u.IsActive, cancellationToken);
        }

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("Invalid email or password.");

        // If 2FA is enabled, return partial response with temporary token
        if (user.TwoFactorEnabled)
        {
            var twoFactorToken = _tokenService.GenerateTwoFactorToken(user, _tenantContext.Tenant!);
            return Result<LoginResponse>.Success(new LoginResponse(
                RequiresTwoFactor: true,
                TwoFactorToken: twoFactorToken,
                Auth: null));
        }

        user.LastLoginAt = DateTime.UtcNow;

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            TenantId = _tenantContext.TenantId,
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(90), // 90-day persistent login
            CreatedBy = user.Username
        };
        await _db.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, _tenantContext.Tenant!);

        return Result<LoginResponse>.Success(new LoginResponse(
            RequiresTwoFactor: false,
            TwoFactorToken: null,
            Auth: new AuthResponse(
                accessToken,
                refreshTokenValue,
                new UserDto(user.Id, user.Username, user.Email, user.Name, user.Phone, user.Role, user.CustomFieldsJson)
            ),
            MustChangePassword: user.MustChangePassword));
    }
}
