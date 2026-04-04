using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Application.Auth.Commands;
using Shop.Application.Auth.Queries;
using Shop.Application.Common.DTOs;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IShopDbContext _db;
    private readonly ILogger<AuthController> _logger;
    private readonly ISecurityMonitorService _security;

    public AuthController(IMediator mediator, ICurrentUserService currentUser, IShopDbContext db, ILogger<AuthController> logger, ISecurityMonitorService security)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
        _security = security;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        // AI-SOC: Check if login should be blocked (IP blocked or account locked)
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (await _security.ShouldBlockLoginAsync(command.Email, clientIp, ct))
        {
            await _security.RecordEventAsync("LoginFailed", "High", clientIp,
                Request.Headers.UserAgent, null, null, "/api/auth/login",
                $"Login blocked for {command.Email} (IP or account locked)", ct: ct);
            return StatusCode(403, new { error = "Account is locked or IP is blocked. Please try again later." });
        }

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
        {
            // Record failed login for AI-SOC pattern detection
            await _security.RecordEventAsync("LoginFailed", "Low", clientIp,
                Request.Headers.UserAgent, null, null, "/api/auth/login",
                $"Login failed for {command.Email}: {result.Error}", ct: ct);
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMeQuery(_currentUser.UserId.Value), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPut("baptismal-name")]
    public async Task<IActionResult> UpdateBaptismalName([FromBody] UpdateBaptismalNameRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new UpdateBaptismalNameCommand(_currentUser.UserId.Value, request.BaptismalName), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("oauth/{provider}")]
    public async Task<IActionResult> OAuthLogin(string provider, [FromBody] OAuthLoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new OAuthLoginCommand(provider, request.Code, request.RedirectUri), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // ── Password Reset ──

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        if (!result.IsSuccess)
            _logger.LogWarning("ForgotPassword failed for {Email}: {Error}", request.Email, result.Error);
        // Always return 200 to prevent email enumeration
        return Ok(new { message = "Password reset link has been sent to your email." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Password has been reset successfully." });
    }

    // ── Email Verification ──

    [Authorize]
    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerificationEmail(CancellationToken ct)
    {
        var result = await _mediator.Send(new SendVerificationEmailCommand(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Verification email has been sent." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyEmailCommand(request.Email, request.Token), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { verified = true });
    }

    // ── Profile ──

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProfileCommand(request.Name, request.Phone), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { changed = true });
    }

    // ── 2FA Endpoints ──

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor(CancellationToken ct)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new EnableTwoFactorCommand(_currentUser.UserId.Value), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/verify-setup")]
    public async Task<IActionResult> VerifyTwoFactorSetup([FromBody] VerifyTwoFactorSetupRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new VerifyTwoFactorSetupCommand(_currentUser.UserId.Value, request.Code), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new DisableTwoFactorCommand(_currentUser.UserId.Value, request.Code), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { disabled = true });
    }

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactorLogin([FromBody] VerifyTwoFactorLoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyTwoFactorLoginCommand(request.TwoFactorToken, request.Code), ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }
    /// <summary>Get active sessions (devices)</summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var sessions = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId.Value && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.CreatedAt,
                t.ExpiresAt,
                daysRemaining = (int)(t.ExpiresAt - DateTime.UtcNow).TotalDays,
                tenantId = t.TenantId
            })
            .ToListAsync(ct);

        return Ok(new { activeSessions = sessions.Count, sessions });
    }

    /// <summary>Revoke a specific session (logout from one device)</summary>
    [HttpDelete("sessions/{tokenId}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(int tokenId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var token = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId.Value, ct);

        if (token == null) return NotFound();

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Session revoked" });
    }

    /// <summary>Revoke all sessions except current (logout from all other devices)</summary>
    [HttpPost("sessions/revoke-others")]
    [Authorize]
    public async Task<IActionResult> RevokeOtherSessions([FromBody] RevokeOthersRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId == null) return Unauthorized();

        var tokens = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId.Value && !t.IsRevoked && t.Token != req.CurrentRefreshToken)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = $"{tokens.Count} sessions revoked" });
    }
}

public record OAuthLoginRequest(string Code, string RedirectUri);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record VerifyEmailRequest(string Email, string Token);
public record UpdateProfileRequest(string? Name, string? Phone);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record VerifyTwoFactorSetupRequest(string Code);
public record DisableTwoFactorRequest(string Code);
public record VerifyTwoFactorLoginRequest(string TwoFactorToken, string Code);
public record RevokeOthersRequest(string CurrentRefreshToken);
