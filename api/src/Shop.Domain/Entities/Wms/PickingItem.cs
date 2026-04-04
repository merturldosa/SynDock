using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_PickingItems")]
public class PickingItem : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int PickingOrderId { get; set; }
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public int LocationId { get; set; }

    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Picked, ShortPick, Skipped

    public DateTime? PickedAt { get; set; }

    [MaxLength(200)]
    public string? BarcodeScanned { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("PickingOrderId")]
    public virtual PickingOrder PickingOrder { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("LocationId")]
    public virtual WarehouseLocation Location { get; set; } = null!;
}
