using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "PlatformAdmin")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityMonitorService _security;
    private readonly ICurrentUserService _currentUser;

    public SecurityController(ISecurityMonitorService security, ICurrentUserService currentUser)
    {
        _security = security;
        _currentUser = currentUser;
    }

    /// <summary>Security dashboard with threat level, event counts, blocked IPs</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _security.GetSecurityDashboardAsync(ct);
        return Ok(dashboard);
    }

    /// <summary>Recent security events, optionally filtered by severity</summary>
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents([FromQuery] string? severity, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var events = await _security.GetRecentEventsAsync(severity, Math.Min(limit, 500), ct);
        return Ok(events);
    }

    /// <summary>Resolve a security event</summary>
    [HttpPost("events/{id}/resolve")]
    public async Task<IActionResult> ResolveEvent(int id, [FromBody] ResolveEventRequest request, CancellationToken ct)
    {
        var resolvedBy = _currentUser.Username ?? "PlatformAdmin";
        await _security.ResolveEventAsync(id, resolvedBy, request.Notes, ct);
        return Ok(new { message = "Event resolved" });
    }

    /// <summary>List all currently blocked IPs</summary>
    [HttpGet("blocked-ips")]
    public async Task<IActionResult> GetBlockedIps(CancellationToken ct)
    {
        var ips = await _security.GetBlockedIpsAsync(ct);
        return Ok(ips);
    }

    /// <summary>Manually block an IP address</summary>
    [HttpPost("block-ip")]
    public async Task<IActionResult> BlockIp([FromBody] BlockIpRequest request, CancellationToken ct)
    {
        DateTime? expiresAt = request.BlockType == "Temporary" && request.DurationHours.HasValue
            ? DateTime.UtcNow.AddHours(request.DurationHours.Value)
            : null;

        await _security.BlockIpAsync(request.IpAddress, request.Reason ?? "Manual block by admin",
            request.BlockType ?? "Permanent", null, expiresAt, ct);

        await _security.RecordEventAsync("IpBlocked", "Medium", request.IpAddress, null, null, null, null,
            $"IP manually blocked by {_currentUser.Username}: {request.Reason}", ct: ct);

        return Ok(new { message = $"IP {request.IpAddress} blocked" });
    }

    /// <summary>Unblock an IP address</summary>
    [HttpDelete("block-ip/{ip}")]
    public async Task<IActionResult> UnblockIp(string ip, CancellationToken ct)
    {
        await _security.UnblockIpAsync(ip, ct);
        return Ok(new { message = $"IP {ip} unblocked" });
    }

    /// <summary>List all currently locked accounts</summary>
    [HttpGet("locked-accounts")]
    public async Task<IActionResult> GetLockedAccounts(CancellationToken ct)
    {
        var accounts = await _security.GetLockedAccountsAsync(ct);
        return Ok(accounts);
    }

    /// <summary>Unlock a user account</summary>
    [HttpPost("unlock/{userId}")]
    public async Task<IActionResult> UnlockAccount(int userId, CancellationToken ct)
    {
        var unlockedBy = _currentUser.Username ?? "PlatformAdmin";
        await _security.UnlockAccountAsync(userId, unlockedBy, ct);
        return Ok(new { message = $"User {userId} unlocked by {unlockedBy}" });
    }

    /// <summary>AI threat pattern analysis (7-day trends, attack vectors, recommendations)</summary>
    [HttpGet("analysis")]
    public async Task<IActionResult> GetAnalysis(CancellationToken ct)
    {
        var analysis = await _security.AnalyzeThreatPatternAsync(ct);
        return Ok(analysis);
    }
}

// Request DTOs
public record ResolveEventRequest(string? Notes);
public record BlockIpRequest(string IpAddress, string? Reason, string? BlockType, int? DurationHours);
