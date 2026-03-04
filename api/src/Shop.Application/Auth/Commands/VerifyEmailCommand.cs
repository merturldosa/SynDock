using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Auth.Commands;

public record VerifyEmailCommand(string Email, string Token) : IRequest<Result<bool>>;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public VerifyEmailCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u =>
                u.Email == request.Email
                && u.EmailVerificationToken == request.Token
                && u.IsActive, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("유효하지 않은 인증 링크입니다.");

        if (user.EmailVerified)
            return Result<bool>.Failure("이미 인증된 이메일입니다.");

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.UpdatedBy = "System";
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
