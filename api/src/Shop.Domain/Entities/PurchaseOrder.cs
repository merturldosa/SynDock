using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PurchaseOrders")]
public class PurchaseOrder : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Created"; // Created, Forwarded, Confirmed, PartialReceived, Received, Cancelled

    [Required]
    [MaxLength(20)]
    public string TriggerType { get; set; } = "Auto"; // Auto, Manual, Forecast

    public int TotalQuantity { get; set; }

    public int ItemCount { get; set; }

    [MaxLength(100)]
    public string? MesOrderId { get; set; }

    [MaxLength(100)]
    public string? MesOrderNo { get; set; }

    public DateTime? ForwardedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? CreatedByUser { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = new();
}

[Table("SP_PurchaseOrderItems")]
public class PurchaseOrderItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int PurchaseOrderId { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? MesProductCode { get; set; }

    public int CurrentStock { get; set; }

    public int ReorderThreshold { get; set; }

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public PurchaseOrder? PurchaseOrder { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
