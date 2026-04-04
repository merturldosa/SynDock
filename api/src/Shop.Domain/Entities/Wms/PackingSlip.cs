using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_PackingSlips")]
public class PackingSlip : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(50)]
    public string PackingNumber { get; set; } = string.Empty;

    public int OrderId { get; set; }
    public int? PickingOrderId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Packing, Packed, Shipped

    public int? PackedByUserId { get; set; }

    [MaxLength(50)]
    public string? TrackingNumber { get; set; }

    [MaxLength(50)]
    public string? CarrierName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalWeight { get; set; }

    [MaxLength(50)]
    public string? BoxSize { get; set; } // S, M, L, XL, Custom

    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("PickingOrderId")]
    public virtual PickingOrder? PickingOrder { get; set; }

    [ForeignKey("PackedByUserId")]
    public virtual User? PackedByUser { get; set; }
}
