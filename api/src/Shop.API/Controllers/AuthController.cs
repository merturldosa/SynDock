using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Application.Auth.Commands;
using Shop.Application.Auth.Queries;
using Shop.Application.Common.DTOs;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ICurrentUserService currentUser, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMeQuery(_currentUser.UserId.Value));
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPut("baptismal-name")]
    public async Task<IActionResult> UpdateBaptismalName([FromBody] UpdateBaptismalNameRequest request)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new UpdateBaptismalNameCommand(_currentUser.UserId.Value, request.BaptismalName));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPost("oauth/{provider}")]
    public async Task<IActionResult> OAuthLogin(string provider, [FromBody] OAuthLoginRequest request)
    {
        var result = await _mediator.Send(new OAuthLoginCommand(provider, request.Code, request.RedirectUri));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // ── Password Reset ──

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email));
        if (!result.IsSuccess)
            _logger.LogWarning("ForgotPassword failed for {Email}: {Error}", request.Email, result.Error);
        // Always return 200 to prevent email enumeration
        return Ok(new { message = "비밀번호 재설정 링크가 이메일로 전송되었습니다." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "비밀번호가 성공적으로 변경되었습니다." });
    }

    // ── Email Verification ──

    [Authorize]
    [HttpPost("send-verification")]
    public async Task<IActionResult> SendVerificationEmail()
    {
        var result = await _mediator.Send(new SendVerificationEmailCommand());
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "인증 메일이 전송되었습니다." });
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _mediator.Send(new VerifyEmailCommand(request.Email, request.Token));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { verified = true });
    }

    // ── Profile ──

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var result = await _mediator.Send(new UpdateProfileCommand(request.Name, request.Phone));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { changed = true });
    }

    // ── 2FA Endpoints ──

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor()
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new EnableTwoFactorCommand(_currentUser.UserId.Value));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/verify-setup")]
    public async Task<IActionResult> VerifyTwoFactorSetup([FromBody] VerifyTwoFactorSetupRequest request)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new VerifyTwoFactorSetupCommand(_currentUser.UserId.Value, request.Code));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        if (_currentUser.UserId == null)
            return Unauthorized();

        var result = await _mediator.Send(new DisableTwoFactorCommand(_currentUser.UserId.Value, request.Code));
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { disabled = true });
    }

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactorLogin([FromBody] VerifyTwoFactorLoginRequest request)
    {
        var result = await _mediator.Send(new VerifyTwoFactorLoginCommand(request.TwoFactorToken, request.Code));
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
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
