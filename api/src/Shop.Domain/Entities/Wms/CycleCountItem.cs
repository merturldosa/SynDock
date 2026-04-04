using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CycleCountItems")]
public class CycleCountItem : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int CycleCountId { get; set; }
    public int ProductId { get; set; }
    public int? LocationId { get; set; }
    public int SystemQuantity { get; set; }
    public int? CountedQuantity { get; set; }
    public int Discrepancy { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, Counted, Verified
    public DateTime? CountedAt { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("CycleCountId")] public virtual CycleCount CycleCount { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product Product { get; set; } = null!;
    [ForeignKey("LocationId")] public virtual WarehouseLocation? Location { get; set; }
}
