using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DeliveryZones")]
public class DeliveryZone : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public double CenterLatitude { get; set; }

    public double CenterLongitude { get; set; }

    public double RadiusKm { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<DeliveryZoneDriver> ZoneDrivers { get; set; } = new List<DeliveryZoneDriver>();
}
