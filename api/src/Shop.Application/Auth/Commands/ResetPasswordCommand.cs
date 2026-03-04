using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Auth.Commands;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result<bool>>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public ResetPasswordCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Email == request.Email
                && u.PasswordResetToken == request.Token
                && u.IsActive, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("유효하지 않은 재설정 링크입니다.");

        if (user.PasswordResetTokenExpiry is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return Result<bool>.Failure("재설정 링크가 만료되었습니다. 다시 요청해 주세요.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedBy = "System";
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
