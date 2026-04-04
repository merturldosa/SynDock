using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_WarehouseZones")]
public class WarehouseZone : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty; // e.g. "A", "B", "COLD"

    [MaxLength(20)]
    public string Type { get; set; } = "General"; // General, Cold, Hazardous, Quarantine, Shipping, Receiving

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
}
