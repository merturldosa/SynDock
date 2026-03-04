using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PushSubscriptions")]
public class PushSubscription : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Endpoint { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? P256dh { get; set; }

    [MaxLength(200)]
    public string? Auth { get; set; }

    [MaxLength(50)]
    public string? UserAgent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastUsedAt { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
