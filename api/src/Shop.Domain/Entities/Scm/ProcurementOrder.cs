using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ProcurementOrders")]
public class ProcurementOrder : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string OrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Draft"; // Draft, Submitted, Confirmed, Shipped, Delivered, Cancelled
    [Column(TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "KRW";
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    [MaxLength(50)] public string? TrackingNumber { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public int? ApprovedByUserId { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("SupplierId")] public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<ProcurementOrderItem> Items { get; set; } = new List<ProcurementOrderItem>();
}
