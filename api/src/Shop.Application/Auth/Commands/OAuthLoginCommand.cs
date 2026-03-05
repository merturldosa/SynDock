using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record OAuthLoginCommand(string Provider, string Code, string RedirectUri) : IRequest<Result<AuthResponse>>;

public class OAuthLoginCommandHandler : IRequestHandler<OAuthLoginCommand, Result<AuthResponse>>
{
    private readonly IShopDbContext _db;
    private readonly IOAuthProviderService _oauthService;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public OAuthLoginCommandHandler(
        IShopDbContext db,
        IOAuthProviderService oauthService,
        ITokenService tokenService,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _oauthService = oauthService;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(OAuthLoginCommand request, CancellationToken cancellationToken)
    {
        OAuthProfile profile;
        try
        {
            profile = await _oauthService.ExchangeCodeForProfile(request.Provider, request.Code, request.RedirectUri, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<AuthResponse>.Failure($"OAuth authentication failed: {ex.Message}");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == profile.Email && u.IsActive, cancellationToken);

        if (user != null)
        {
            // Existing user — update OAuth info
            var oauthData = new
            {
                oauthProvider = profile.Provider,
                oauthProviderId = profile.ProviderId,
                profileImageUrl = profile.ProfileImageUrl
            };

            // Merge OAuth data into existing CustomFieldsJson
            var existingFields = !string.IsNullOrEmpty(user.CustomFieldsJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(user.CustomFieldsJson) ?? new()
                : new Dictionary<string, object>();

            existingFields["oauthProvider"] = profile.Provider;
            existingFields["oauthProviderId"] = profile.ProviderId;
            if (profile.ProfileImageUrl != null)
                existingFields["profileImageUrl"] = profile.ProfileImageUrl;

            user.CustomFieldsJson = JsonSerializer.Serialize(existingFields);
            user.LastLoginAt = DateTime.UtcNow;
        }
        else
        {
            // New user — create account
            var randomSuffix = Guid.NewGuid().ToString("N")[..6];
            var username = $"{profile.Email.Split('@')[0]}_{randomSuffix}";

            user = new User
            {
                TenantId = _tenantContext.TenantId,
                Username = username.Length > 50 ? username[..50] : username,
                Email = profile.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                Name = profile.Name,
                Role = nameof(UserRole.Member),
                IsActive = true,
                LastLoginAt = DateTime.UtcNow,
                CustomFieldsJson = JsonSerializer.Serialize(new
                {
                    oauthProvider = profile.Provider,
                    oauthProviderId = profile.ProviderId,
                    profileImageUrl = profile.ProfileImageUrl
                }),
                CreatedBy = "oauth"
            };

            await _db.Users.AddAsync(user, cancellationToken);
        }

        // Generate refresh token
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
}
