using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Auth.Commands;

public record SendVerificationEmailCommand : IRequest<Result<bool>>;

public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;

    public SendVerificationEmailCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IEmailService emailService)
    {
        _db = db;
        _currentUser = currentUser;
        _emailService = emailService;
    }

    public async Task<Result<bool>> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        if (user.EmailVerified)
            return Result<bool>.Failure("Email is already verified.");

        var token = Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = token;
        user.UpdatedBy = "System";
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var verifyLink = $"/auth/verify-email?token={token}&email={Uri.EscapeDataString(user.Email)}";
        var subject = "[SynDock] 이메일 인증 안내";
        var body = BuildVerificationEmail(user.Name, verifyLink);

        try
        {
            await _emailService.SendAsync(user.Email, subject, body, cancellationToken);
        }
        catch { /* 이메일 실패는 무시 */ }

        return Result<bool>.Success(true);
    }

    private static string BuildVerificationEmail(string name, string verifyLink)
    {
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:0;background:#f5f5f5;font-family:sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#fff;"">
<tr><td style=""background:#D4AF37;padding:24px 32px;""><h1 style=""margin:0;color:#fff;font-size:20px;"">이메일 인증</h1></td></tr>
<tr><td style=""padding:32px;"">
<p style=""color:#666;line-height:1.6;"">안녕하세요, <strong>{name}</strong>님.</p>
<p style=""color:#666;line-height:1.6;"">아래 버튼을 클릭하여 이메일 주소를 인증해 주세요.</p>
<div style=""text-align:center;margin:24px 0;"">
<a href=""{verifyLink}"" style=""display:inline-block;padding:12px 32px;background:#D4AF37;color:#fff;text-decoration:none;border-radius:6px;font-weight:bold;"">이메일 인증</a>
</div>
<p style=""color:#999;font-size:13px;"">본인이 요청하지 않았다면 이 메일을 무시해 주세요.</p>
</td></tr>
<tr><td style=""padding:16px 32px;background:#f9f9f9;text-align:center;font-size:12px;color:#999;"">본 메일은 발신 전용입니다.</td></tr>
</table></body></html>";
    }
}
