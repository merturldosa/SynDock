using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Gifts")]
public class Gift : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    [Required, MaxLength(20)] public string GiftType { get; set; } = string.Empty; // SDT, Points, Coupon, Badge
    [Column(TypeName = "decimal(18,4)")] public decimal Amount { get; set; }
    [MaxLength(200)] public string? Message { get; set; } // Gift message
    [MaxLength(50)] public string? TriggerType { get; set; } // Review, Post, Referral, Birthday, Manual
    public int? TriggerReferenceId { get; set; } // Review ID, Post ID, etc.
    [Required, MaxLength(20)] public string Status { get; set; } = "Sent"; // Sent, Received, Thanked
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ThankedAt { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("FromUserId")] public virtual User FromUser { get; set; } = null!;
    [ForeignKey("ToUserId")] public virtual User ToUser { get; set; } = null!;
}
