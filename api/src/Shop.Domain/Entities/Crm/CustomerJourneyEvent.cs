using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CustomerJourneyEvents")]
public class CustomerJourneyEvent : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // PageView, ProductView, AddToCart, Purchase, Review, Return, CsTicket, CouponUsed, PointEarned

    [MaxLength(200)]
    public string? EventDetail { get; set; } // e.g. product name, page URL

    public int? ReferenceId { get; set; } // Related entity ID (productId, orderId, etc.)

    [MaxLength(50)]
    public string? ReferenceType { get; set; } // Product, Order, CsTicket, etc.

    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    [MaxLength(50)]
    public string? Channel { get; set; } // Web, Mobile, API

    [MaxLength(50)]
    public string? SessionId { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
