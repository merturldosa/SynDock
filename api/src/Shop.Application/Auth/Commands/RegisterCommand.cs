using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public RegisterCommandHandler(IShopDbContext db, ITokenService tokenService, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
            return Result<AuthResponse>.Failure("이미 사용 중인 사용자명입니다.");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Failure("이미 사용 중인 이메일입니다.");

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
