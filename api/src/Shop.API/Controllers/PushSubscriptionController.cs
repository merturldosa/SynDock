using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/push")]
[Authorize]
public class PushSubscriptionController : ControllerBase
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PushSubscriptionController(
        IShopDbContext db,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [HttpGet("vapid-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _configuration["WebPush:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
            return NotFound(new { error = "WebPush not configured" });

        return Ok(new { publicKey });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "User not authenticated" });
        var userId = _currentUser.UserId.Value;

        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

        if (existing != null)
        {
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
            existing.UpdatedBy = _currentUser.Username;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                UserAgent = Request.Headers.UserAgent.ToString()[..Math.Min(50, Request.Headers.UserAgent.ToString().Length)],
                IsActive = true,
                LastUsedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username ?? "system"
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "User not authenticated" });
        var userId = _currentUser.UserId.Value;

        var subscription = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Endpoint);

        if (subscription != null)
        {
            subscription.IsActive = false;
            subscription.UpdatedBy = _currentUser.Username;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Ok(new { success = true });
    }

    /// <summary>모바일 앱 FCM/Expo 푸시 토큰 등록</summary>
    [HttpPost("mobile-token")]
    public async Task<IActionResult> RegisterMobileToken([FromBody] MobilePushTokenRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "User not authenticated" });
        var userId = _currentUser.UserId.Value;

        var existing = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Token, ct);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LastUsedAt = DateTime.UtcNow;
            existing.UserAgent = request.Platform;
            existing.UpdatedBy = _currentUser.Username;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Token,
                UserAgent = request.Platform,
                IsActive = true,
                LastUsedAt = DateTime.UtcNow,
                CreatedBy = _currentUser.Username ?? "system"
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    /// <summary>모바일 앱 푸시 토큰 해제</summary>
    [HttpPost("mobile-token/unregister")]
    public async Task<IActionResult> UnregisterMobileToken([FromBody] MobilePushTokenRequest request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Unauthorized(new { error = "User not authenticated" });
        var userId = _currentUser.UserId.Value;

        var subscription = await _db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == request.Token, ct);

        if (subscription is not null)
        {
            subscription.IsActive = false;
            subscription.UpdatedBy = _currentUser.Username;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Ok(new { success = true });
    }
}

public record PushSubscribeRequest(string Endpoint, string? P256dh, string? Auth);
public record PushUnsubscribeRequest(string Endpoint);
public record MobilePushTokenRequest(string Token, string Platform);
