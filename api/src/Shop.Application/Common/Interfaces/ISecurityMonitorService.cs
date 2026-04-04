using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface ISecurityMonitorService
{
    // Event Recording
    Task<SecurityEvent> RecordEventAsync(string eventType, string severity, string? clientIp, string? userAgent, int? userId, int? tenantId, string? requestPath, string description, string? metadataJson = null, CancellationToken ct = default);

    // Threat Detection
    Task<bool> IsIpBlockedAsync(string ip, CancellationToken ct = default);
    Task<bool> IsAccountLockedAsync(string email, CancellationToken ct = default);
    Task<bool> ShouldBlockLoginAsync(string email, string? clientIp, CancellationToken ct = default);

    // Auto-Response
    Task BlockIpAsync(string ip, string reason, string blockType, int? securityEventId, DateTime? expiresAt = null, CancellationToken ct = default);
    Task UnblockIpAsync(string ip, CancellationToken ct = default);
    Task LockAccountAsync(int userId, string email, string reason, DateTime? lockedUntil = null, string? lastIp = null, CancellationToken ct = default);
    Task UnlockAccountAsync(int userId, string unlockedBy, CancellationToken ct = default);

    // Analysis
    Task<object> GetSecurityDashboardAsync(CancellationToken ct = default);
    Task<List<SecurityEvent>> GetRecentEventsAsync(string? severity = null, int limit = 100, CancellationToken ct = default);
    Task<List<BlockedIp>> GetBlockedIpsAsync(CancellationToken ct = default);
    Task<List<AccountLockout>> GetLockedAccountsAsync(CancellationToken ct = default);
    Task ResolveEventAsync(int eventId, string resolvedBy, string? notes, CancellationToken ct = default);

    // AI Analysis
    Task<object> AnalyzeThreatPatternAsync(CancellationToken ct = default);
}
