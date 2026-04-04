using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_GoodsReceiptItems")]
public class GoodsReceiptItem : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [Required, MaxLength(20)] public string QualityStatus { get; set; } = "Pending"; // Pending, Pass, Fail, Partial
    [MaxLength(500)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("GoodsReceiptId")] public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product Product { get; set; } = null!;
}
