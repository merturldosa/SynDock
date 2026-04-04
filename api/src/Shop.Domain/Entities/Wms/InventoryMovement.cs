using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_InventoryMovements")]
public class InventoryMovement : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int ProductId { get; set; }
    public int? VariantId { get; set; }

    [Required, MaxLength(20)]
    public string MovementType { get; set; } = string.Empty; // Inbound, Outbound, Transfer, Adjustment, Return

    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }

    public int? FromLocationId { get; set; }
    public int? ToLocationId { get; set; }

    public int? OrderId { get; set; }
    public int? PurchaseOrderId { get; set; }

    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("FromLocationId")]
    public virtual WarehouseLocation? FromLocation { get; set; }

    [ForeignKey("ToLocationId")]
    public virtual WarehouseLocation? ToLocation { get; set; }
}
