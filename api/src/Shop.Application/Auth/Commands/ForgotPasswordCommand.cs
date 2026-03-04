using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IShopDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        // 보안: 존재 여부와 관계없이 동일한 응답 (이메일 열거 방지)
        if (user is null)
            return Result<bool>.Success(true);

        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedBy = "System";
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var resetLink = $"/auth/reset-password?token={token}&email={Uri.EscapeDataString(request.Email)}";
        var subject = "[SynDock] 비밀번호 재설정 안내";
        var body = BuildPasswordResetEmail(user.Name, resetLink);

        try
        {
            await _emailService.SendAsync(user.Email, subject, body, cancellationToken);
        }
        catch { /* 이메일 실패는 무시 */ }

        return Result<bool>.Success(true);
    }

    private static string BuildPasswordResetEmail(string name, string resetLink)
    {
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:0;background:#f5f5f5;font-family:sans-serif;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#fff;"">
<tr><td style=""background:#D4AF37;padding:24px 32px;""><h1 style=""margin:0;color:#fff;font-size:20px;"">비밀번호 재설정</h1></td></tr>
<tr><td style=""padding:32px;"">
<p style=""color:#666;line-height:1.6;"">안녕하세요, <strong>{name}</strong>님.</p>
<p style=""color:#666;line-height:1.6;"">비밀번호 재설정을 요청하셨습니다. 아래 버튼을 클릭하여 새 비밀번호를 설정해 주세요.</p>
<div style=""text-align:center;margin:24px 0;"">
<a href=""{resetLink}"" style=""display:inline-block;padding:12px 32px;background:#D4AF37;color:#fff;text-decoration:none;border-radius:6px;font-weight:bold;"">비밀번호 재설정</a>
</div>
<p style=""color:#999;font-size:13px;"">이 링크는 1시간 동안 유효합니다.</p>
<p style=""color:#999;font-size:13px;"">본인이 요청하지 않았다면 이 메일을 무시해 주세요.</p>
</td></tr>
<tr><td style=""padding:16px 32px;background:#f9f9f9;text-align:center;font-size:12px;color:#999;"">본 메일은 발신 전용입니다.</td></tr>
</table></body></html>";
    }
}
