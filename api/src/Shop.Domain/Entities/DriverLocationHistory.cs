using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DriverLocationHistories")]
public class DriverLocationHistory : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int DeliveryDriverId { get; set; }

    public int? DeliveryAssignmentId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("DeliveryDriverId")]
    public DeliveryDriver Driver { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
