using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_LotTrackings")]
public class LotTracking : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int ProductId { get; set; }
    [Required, MaxLength(50)] public string LotNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? BatchNumber { get; set; }
    public DateTime? ManufacturedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Quantity { get; set; }
    public int? LocationId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Available"; // Available, Reserved, Expired, Quarantine
    [MaxLength(500)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product Product { get; set; } = null!;
    [ForeignKey("LocationId")] public virtual WarehouseLocation? Location { get; set; }
}
