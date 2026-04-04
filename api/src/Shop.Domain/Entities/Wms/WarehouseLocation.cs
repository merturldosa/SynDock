using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_WarehouseLocations")]
public class WarehouseLocation : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int WarehouseZoneId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty; // e.g. "A-01-02" (Zone-Row-Shelf)

    [MaxLength(20)]
    public string Type { get; set; } = "Shelf"; // Shelf, Bin, Floor, Pallet

    public int? ProductId { get; set; }
    public int CurrentQuantity { get; set; }
    public int MaxCapacity { get; set; } = 1000;

    public bool IsActive { get; set; } = true;
    public bool IsOccupied { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("WarehouseZoneId")]
    public virtual WarehouseZone Zone { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
