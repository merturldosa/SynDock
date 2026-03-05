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
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

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
