using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ProcurementOrderItems")]
public class ProcurementOrderItem : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int ProcurementOrderId { get; set; }
    public int ProductId { get; set; }
    [MaxLength(200)] public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal TotalPrice { get; set; }
    public int ReceivedQuantity { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("ProcurementOrderId")] public virtual ProcurementOrder ProcurementOrder { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product Product { get; set; } = null!;
}
