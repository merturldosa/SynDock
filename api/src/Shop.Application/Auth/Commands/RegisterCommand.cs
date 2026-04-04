using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.DTOs;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record RegisterCommand(
    string Username,
    string Email,
    string Password,
    string Name,
    string? Phone,
    string? CustomFieldsJson
) : IRequest<Result<AuthResponse>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanEnforcer _planEnforcer;
    private readonly IAutoCouponService _autoCoupon;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(IShopDbContext db, ITokenService tokenService, ITenantContext tenantContext, IUnitOfWork unitOfWork, IPlanEnforcer planEnforcer, IAutoCouponService autoCoupon, IEmailService emailService, ILogger<RegisterCommandHandler> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _planEnforcer = planEnforcer;
        _autoCoupon = autoCoupon;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Plan limit check
        var limitCheck = await _planEnforcer.CanRegisterUser(_tenantContext.TenantId, cancellationToken);
        if (!limitCheck.IsSuccess)
            return Result<AuthResponse>.Failure(limitCheck.Error!);

        if (await _db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            return Result<AuthResponse>.Failure("Username is already in use.");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Failure("Email is already in use.");

        // SSO: Create platform user (TenantId=0) for cross-tenant access
        var platformUser = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == 0, cancellationToken);

        if (platformUser == null)
        {
            platformUser = new User
            {
                TenantId = 0, // Platform user - works across all tenant shops
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Name = request.Name,
                Phone = request.Phone,
                Role = nameof(UserRole.Member),
                CustomFieldsJson = request.CustomFieldsJson,
                CreatedBy = "SSO-Platform"
            };
            await _db.Users.AddAsync(platformUser, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Also create tenant-specific user for this shop (linked by email)
        var user = new User
        {
            TenantId = _tenantContext.TenantId,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = request.Name,
            Phone = request.Phone,
            Role = nameof(UserRole.Member),
            CustomFieldsJson = request.CustomFieldsJson,
            CreatedBy = request.Username
        };

        await _db.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Track usage
        await _planEnforcer.IncrementUserCount(_tenantContext.TenantId, 1, cancellationToken);

        // Issue welcome coupon (fire-and-forget style, don't fail registration)
        try
        {
            await _autoCoupon.IssueWelcomeCouponAsync(_tenantContext.TenantId, user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Welcome coupon issuance failed for user {UserId}", user.Id); }

        // Send welcome email (fire-and-forget style)
        try
        {
            var tenantName = _tenantContext.Tenant?.Name ?? "Shop";
            var body = $"<h2>Welcome to {tenantName}!</h2><p>Hello {user.Name}, thank you for joining us. Start exploring our products!</p>";
            await _emailService.SendAsync(user.Email, $"Welcome to {tenantName}!", body, CancellationToken.None);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Welcome email failed for user {UserId}", user.Id); }

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            TenantId = _tenantContext.TenantId,
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = request.Username
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
