using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class SecurityMonitorService : ISecurityMonitorService
{
    private readonly IShopDbContext _db;
    private readonly ILogger<SecurityMonitorService> _logger;

    public SecurityMonitorService(IShopDbContext db, ILogger<SecurityMonitorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SecurityEvent> RecordEventAsync(string eventType, string severity, string? clientIp, string? userAgent, int? userId, int? tenantId, string? requestPath, string description, string? metadataJson = null, CancellationToken ct = default)
    {
        var evt = new SecurityEvent
        {
            EventType = eventType,
            Severity = severity,
            ClientIp = clientIp,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            UserId = userId,
            TenantId = tenantId,
            RequestPath = requestPath,
            Description = description,
            ActionTaken = "Logged",
            MetadataJson = metadataJson,
            CreatedBy = "AI-SOC"
        };

        _db.SecurityEvents.Add(evt);
        await _db.SaveChangesAsync(ct);

        // Auto-escalation based on patterns
        if (!string.IsNullOrEmpty(clientIp))
            await AutoEscalateAsync(evt, clientIp, userId, ct);

        if (severity == "High" || severity == "Critical")
            _logger.LogWarning("SECURITY [{Severity}] {Type}: {Description} | IP={IP} User={User}", severity, eventType, description, clientIp, userId);

        return evt;
    }

    private async Task AutoEscalateAsync(SecurityEvent evt, string clientIp, int? userId, CancellationToken ct)
    {
        var tenMinAgo = DateTime.UtcNow.AddMinutes(-10);

        // Pattern: SQL Injection or XSS -> immediate permanent block
        if (evt.EventType is "SqlInjection" or "XssAttempt")
        {
            await BlockIpAsync(clientIp, $"Auto-blocked: {evt.EventType} detected", "Permanent", evt.Id, null, ct);
            evt.ActionTaken = "IpBlocked";
            await _db.SaveChangesAsync(ct);
            _logger.LogCritical("AI-SOC: PERMANENT IP BLOCK {IP} -- {Type}", clientIp, evt.EventType);
            return;
        }

        // Pattern: 5+ failed logins from same IP in 10 min -> temp block (1 hour)
        if (evt.EventType == "LoginFailed")
        {
            var recentFailures = await _db.SecurityEvents
                .CountAsync(e => e.ClientIp == clientIp && e.EventType == "LoginFailed" && e.CreatedAt > tenMinAgo, ct);

            if (recentFailures >= 5)
            {
                var alreadyBlocked = await IsIpBlockedAsync(clientIp, ct);
                if (!alreadyBlocked)
                {
                    await BlockIpAsync(clientIp, $"Auto-blocked: {recentFailures} failed logins in 10 min (brute force)", "Temporary", evt.Id, DateTime.UtcNow.AddHours(1), ct);
                    evt.ActionTaken = "IpBlocked";
                    evt.Severity = "High";
                    _logger.LogWarning("AI-SOC: TEMP IP BLOCK {IP} -- {Count} failed logins (brute force)", clientIp, recentFailures);
                }
            }

            // Pattern: 10+ failed logins for same user -> lock account
            if (userId.HasValue)
            {
                var userFailures = await _db.SecurityEvents
                    .CountAsync(e => e.UserId == userId.Value && e.EventType == "LoginFailed" && e.CreatedAt > DateTime.UtcNow.AddMinutes(-30), ct);

                if (userFailures >= 10)
                {
                    var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                    if (user != null)
                    {
                        await LockAccountAsync(userId.Value, user.Email, $"Auto-locked: {userFailures} failed logins in 30 min", DateTime.UtcNow.AddMinutes(30), clientIp, ct);
                        evt.ActionTaken = "AccountLocked";
                        _logger.LogWarning("AI-SOC: ACCOUNT LOCKED {Email} -- {Count} failed logins", user.Email, userFailures);
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        // Pattern: Rate limit abuse -> temp block
        if (evt.EventType == "RateLimitHit")
        {
            var recentHits = await _db.SecurityEvents
                .CountAsync(e => e.ClientIp == clientIp && e.EventType == "RateLimitHit" && e.CreatedAt > tenMinAgo, ct);

            if (recentHits >= 10)
            {
                await BlockIpAsync(clientIp, $"Auto-blocked: Rate limit hit {recentHits} times in 10 min", "Temporary", evt.Id, DateTime.UtcNow.AddHours(2), ct);
                evt.ActionTaken = "IpBlocked";
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<bool> IsIpBlockedAsync(string ip, CancellationToken ct = default)
    {
        return await _db.BlockedIps.AnyAsync(b => b.IpAddress == ip && b.IsActive && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow), ct);
    }

    public async Task<bool> IsAccountLockedAsync(string email, CancellationToken ct = default)
    {
        return await _db.AccountLockouts.AnyAsync(a => a.Email == email && a.Status == "Locked" && (a.LockedUntil == null || a.LockedUntil > DateTime.UtcNow), ct);
    }

    public async Task<bool> ShouldBlockLoginAsync(string email, string? clientIp, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(clientIp) && await IsIpBlockedAsync(clientIp, ct)) return true;
        if (await IsAccountLockedAsync(email, ct)) return true;
        return false;
    }

    public async Task BlockIpAsync(string ip, string reason, string blockType, int? securityEventId, DateTime? expiresAt = null, CancellationToken ct = default)
    {
        var existing = await _db.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == ip && b.IsActive, ct);
        if (existing != null)
        {
            if (blockType == "Permanent") { existing.BlockType = "Permanent"; existing.ExpiresAt = null; }
            return;
        }

        _db.BlockedIps.Add(new BlockedIp
        {
            IpAddress = ip, BlockType = blockType, Reason = reason,
            SecurityEventId = securityEventId, ExpiresAt = expiresAt,
            CreatedBy = "AI-SOC"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnblockIpAsync(string ip, CancellationToken ct = default)
    {
        var blocked = await _db.BlockedIps.Where(b => b.IpAddress == ip && b.IsActive).ToListAsync(ct);
        foreach (var b in blocked) b.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }

    public async Task LockAccountAsync(int userId, string email, string reason, DateTime? lockedUntil = null, string? lastIp = null, CancellationToken ct = default)
    {
        var existing = await _db.AccountLockouts.FirstOrDefaultAsync(a => a.UserId == userId && a.Status == "Locked", ct);
        if (existing != null) { existing.FailedAttempts++; return; }

        _db.AccountLockouts.Add(new AccountLockout
        {
            UserId = userId, Email = email, FailedAttempts = 1,
            Reason = reason, LockedUntil = lockedUntil, LastAttemptIp = lastIp,
            CreatedBy = "AI-SOC"
        });

        // Also deactivate user temporarily
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null) { user.IsActive = false; user.UpdatedBy = "AI-SOC"; user.UpdatedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UnlockAccountAsync(int userId, string unlockedBy, CancellationToken ct = default)
    {
        var lockouts = await _db.AccountLockouts.Where(a => a.UserId == userId && a.Status == "Locked").ToListAsync(ct);
        foreach (var l in lockouts) { l.Status = "Unlocked"; l.UnlockedAt = DateTime.UtcNow; l.UnlockedBy = unlockedBy; }

        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null) { user.IsActive = true; user.UpdatedBy = unlockedBy; user.UpdatedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> GetSecurityDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var lastHour = now.AddHours(-1);
        var last24h = now.AddHours(-24);

        var hourlyEvents = await _db.SecurityEvents.CountAsync(e => e.CreatedAt > lastHour, ct);
        var dailyEvents = await _db.SecurityEvents.CountAsync(e => e.CreatedAt > last24h, ct);

        var threatLevel = hourlyEvents switch
        {
            >= 50 => "Red",
            >= 20 => "Orange",
            >= 5 => "Yellow",
            _ => "Green"
        };

        var bySeverity = await _db.SecurityEvents.Where(e => e.CreatedAt > last24h)
            .GroupBy(e => e.Severity).Select(g => new { severity = g.Key, count = g.Count() }).ToListAsync(ct);
        var byType = await _db.SecurityEvents.Where(e => e.CreatedAt > last24h)
            .GroupBy(e => e.EventType).Select(g => new { type = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(10).ToListAsync(ct);
        var blockedIps = await _db.BlockedIps.CountAsync(b => b.IsActive, ct);
        var lockedAccounts = await _db.AccountLockouts.CountAsync(a => a.Status == "Locked" && (a.LockedUntil == null || a.LockedUntil > now), ct);
        var topAttackerIps = await _db.SecurityEvents.Where(e => e.CreatedAt > last24h && e.Severity != "Low")
            .GroupBy(e => e.ClientIp).Select(g => new { ip = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(5).ToListAsync(ct);

        return new
        {
            threatLevel, hourlyEvents, dailyEvents,
            bySeverity, byType, blockedIps, lockedAccounts, topAttackerIps,
            lastUpdated = now
        };
    }

    public async Task<List<SecurityEvent>> GetRecentEventsAsync(string? severity = null, int limit = 100, CancellationToken ct = default)
    {
        var query = _db.SecurityEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(severity)) query = query.Where(e => e.Severity == severity);
        return await query.OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync(ct);
    }

    public async Task<List<BlockedIp>> GetBlockedIpsAsync(CancellationToken ct = default)
        => await _db.BlockedIps.AsNoTracking().Where(b => b.IsActive).OrderByDescending(b => b.CreatedAt).ToListAsync(ct);

    public async Task<List<AccountLockout>> GetLockedAccountsAsync(CancellationToken ct = default)
        => await _db.AccountLockouts.AsNoTracking().Where(a => a.Status == "Locked").OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task ResolveEventAsync(int eventId, string resolvedBy, string? notes, CancellationToken ct = default)
    {
        var evt = await _db.SecurityEvents.FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (evt == null) return;
        evt.IsResolved = true;
        evt.ResolvedAt = DateTime.UtcNow;
        evt.ResolvedBy = resolvedBy;
        evt.ResolutionNotes = notes;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<object> AnalyzeThreatPatternAsync(CancellationToken ct = default)
    {
        var last7d = DateTime.UtcNow.AddDays(-7);
        var events = await _db.SecurityEvents.Where(e => e.CreatedAt > last7d).AsNoTracking().ToListAsync(ct);

        // Daily trend
        var dailyTrend = events.GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), total = g.Count(), critical = g.Count(e => e.Severity == "Critical"), high = g.Count(e => e.Severity == "High") })
            .OrderBy(x => x.date).ToList();

        // Most common attack vectors
        var attackVectors = events.Where(e => e.Severity != "Low")
            .GroupBy(e => e.EventType).Select(g => new { type = g.Key, count = g.Count(), lastSeen = g.Max(e => e.CreatedAt) })
            .OrderByDescending(x => x.count).ToList();

        // Geographic analysis (by IP prefix)
        var geoAnalysis = events.Where(e => e.ClientIp != null)
            .GroupBy(e =>
            {
                var ip = e.ClientIp!;
                var lastDot = ip.LastIndexOf('.');
                return lastDot > 0 ? ip[..lastDot] + ".*" : ip;
            })
            .Select(g => new { ipRange = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count).Take(10).ToList();

        // AI recommendation
        var totalCritical = events.Count(e => e.Severity == "Critical");
        var totalHigh = events.Count(e => e.Severity == "High");
        var recommendation = (totalCritical, totalHigh) switch
        {
            ( > 10, _) => "CRITICAL: Immediate full security audit required. Multiple critical events detected.",
            (_, > 50) => "WARNING: High-severity events elevated. Recommend strengthening IP block policies.",
            (_, > 20) => "CAUTION: Abnormal access patterns detected. Increase monitoring.",
            _ => "NORMAL: Security posture is healthy. Maintain regular monitoring."
        };

        return new { dailyTrend, attackVectors, geoAnalysis, recommendation, period = "7 days", totalEvents = events.Count };
    }
}
