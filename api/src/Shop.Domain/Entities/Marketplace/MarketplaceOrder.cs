using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_MarketplaceOrders")]
public class MarketplaceOrder : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int MarketplaceConnectionId { get; set; }
    [MaxLength(30)] public string MarketplaceCode { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ExternalOrderId { get; set; } = string.Empty;
    public int? SynDockOrderId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "New";
    [MaxLength(100)] public string? BuyerName { get; set; }
    [MaxLength(200)] public string? BuyerAddress { get; set; }
    [MaxLength(20)] public string? BuyerPhone { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal MarketplaceFee { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal NetAmount { get; set; }
    [Column(TypeName = "jsonb")] public string? OrderItemsJson { get; set; }
    [MaxLength(50)] public string? TrackingNumber { get; set; }
    public DateTime? OrderedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("MarketplaceConnectionId")] public virtual MarketplaceConnection Connection { get; set; } = null!;
}
