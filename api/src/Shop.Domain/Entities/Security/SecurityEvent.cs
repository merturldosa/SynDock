using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_SecurityEvents")]
public class SecurityEvent : BaseEntity
{
    [Required, MaxLength(30)] public string EventType { get; set; } = string.Empty;
    // LoginFailed, LoginSuccess, BruteForce, SqlInjection, XssAttempt,
    // UnusualLocation, RateLimitHit, PartnerAbuse, AccountLocked,
    // IpBlocked, SuspiciousApi, DataExfiltration, TokenStolen
    [Required, MaxLength(10)] public string Severity { get; set; } = "Low"; // Low, Medium, High, Critical
    [MaxLength(50)] public string? ClientIp { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    public int? UserId { get; set; }
    public int? TenantId { get; set; }
    [MaxLength(500)] public string? RequestPath { get; set; }
    [Required, MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string ActionTaken { get; set; } = "Logged"; // Logged, Alerted, IpBlocked, AccountLocked, PartnerSuspended, SystemDefense
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(200)] public string? ResolvedBy { get; set; }
    [MaxLength(500)] public string? ResolutionNotes { get; set; }
    [Column(TypeName = "jsonb")] public string? MetadataJson { get; set; }
}
