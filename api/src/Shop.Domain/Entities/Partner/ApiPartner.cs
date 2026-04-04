using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_ApiPartners")]
public class ApiPartner : BaseEntity
{
    [Required, MaxLength(100)] public string CompanyName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string PartnerCode { get; set; } = string.Empty; // Unique partner identifier
    [Required, MaxLength(64)] public string ApiKey { get; set; } = string.Empty; // Public key (sent in header)
    [Required, MaxLength(128)] public string ApiSecretHash { get; set; } = string.Empty; // Hashed secret (for HMAC verification)
    [Required, MaxLength(20)] public string Status { get; set; } = "Active"; // Active, Suspended, Revoked
    [Required, MaxLength(20)] public string Tier { get; set; } = "Standard"; // Standard, Premium, Enterprise
    [MaxLength(200)] public string? ContactEmail { get; set; }
    [MaxLength(500)] public string? WebhookUrl { get; set; } // Callback URL for order notifications
    [MaxLength(500)] public string? AllowedIps { get; set; } // Comma-separated IP whitelist (null = all)
    public int RateLimitPerMinute { get; set; } = 60; // API calls per minute
    public int RateLimitPerDay { get; set; } = 10000; // API calls per day
    public int DailyCallCount { get; set; }
    public DateTime? DailyCallResetAt { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal CommissionRate { get; set; } = 3.0m; // SynDock commission %
    public bool AutoApproveProducts { get; set; } // Skip manual review
    [MaxLength(500)] public string? Notes { get; set; }
    [Column(TypeName = "jsonb")] public string? PermissionsJson { get; set; } // {"canListProducts":true,"canManageOrders":true,...}
    public DateTime? LastActivityAt { get; set; }
}
