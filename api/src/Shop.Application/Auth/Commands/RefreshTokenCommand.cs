using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(IShopDbContext db, ITokenService tokenService, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    {
        _db = db;
        _tokenService = tokenService;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked, cancellationToken);

        if (token == null || token.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Invalid or expired token.");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        token.ReplacedByToken = newRefreshTokenValue;

        var newRefreshToken = new RefreshToken
        {
            TenantId = _tenantContext.TenantId,
            UserId = token.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = token.User.Username
        };
        await _db.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(token.User, _tenantContext.Tenant!);

        return Result<AuthResponse>.Success(new AuthResponse(
            accessToken,
            newRefreshTokenValue,
            new UserDto(token.User.Id, token.User.Username, token.User.Email, token.User.Name, token.User.Phone, token.User.Role, token.User.CustomFieldsJson)
        ));
    }
}
