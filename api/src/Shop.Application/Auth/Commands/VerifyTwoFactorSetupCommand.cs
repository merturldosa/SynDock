using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record VerifyTwoFactorSetupCommand(int UserId, string Code) : IRequest<Result<TwoFactorVerifySetupResponse>>;

public class VerifyTwoFactorSetupCommandHandler : IRequestHandler<VerifyTwoFactorSetupCommand, Result<TwoFactorVerifySetupResponse>>
{
    private readonly IShopDbContext _db;
    private readonly ITotpService _totpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public VerifyTwoFactorSetupCommandHandler(
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

    public async Task<Result<TwoFactorVerifySetupResponse>> Handle(VerifyTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return Result<TwoFactorVerifySetupResponse>.Failure("사용자를 찾을 수 없습니다.");

        if (user.TwoFactorEnabled)
            return Result<TwoFactorVerifySetupResponse>.Failure("이미 2단계 인증이 활성화되어 있습니다.");

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
            return Result<TwoFactorVerifySetupResponse>.Failure("먼저 2단계 인증 설정을 시작해주세요.");

        // Validate the TOTP code against the stored secret
        if (!_totpService.ValidateCode(user.TwoFactorSecret, request.Code))
            return Result<TwoFactorVerifySetupResponse>.Failure("인증 코드가 올바르지 않습니다.");

        // Activate 2FA
        user.TwoFactorEnabled = true;
        user.UpdatedBy = _currentUser.Username ?? "system";
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TwoFactorVerifySetupResponse>.Success(new TwoFactorVerifySetupResponse(true));
    }
}
