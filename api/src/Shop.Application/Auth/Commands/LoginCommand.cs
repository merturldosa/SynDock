using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
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

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure("이메일 또는 비밀번호가 올바르지 않습니다.");

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
}
