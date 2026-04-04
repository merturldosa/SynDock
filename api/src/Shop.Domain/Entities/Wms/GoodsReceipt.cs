using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_GoodsReceipts")]
public class GoodsReceipt : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string ReceiptNumber { get; set; } = string.Empty;
    public int? PurchaseOrderId { get; set; }
    [MaxLength(100)] public string? SupplierName { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, Inspecting, Accepted, PartialAccept, Rejected
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    public int? InspectedByUserId { get; set; }
    public DateTime? InspectedAt { get; set; }
    [MaxLength(500)] public string? InspectionNotes { get; set; }
    public int? TargetLocationId { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("PurchaseOrderId")] public virtual PurchaseOrder? PurchaseOrder { get; set; }
    [ForeignKey("TargetLocationId")] public virtual WarehouseLocation? TargetLocation { get; set; }
    public virtual ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
}
