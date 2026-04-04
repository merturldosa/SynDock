using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DeliveryZoneDrivers")]
public class DeliveryZoneDriver : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int DeliveryZoneId { get; set; }

    public int DeliveryDriverId { get; set; }

    // Navigation
    [ForeignKey("DeliveryZoneId")]
    public DeliveryZone Zone { get; set; } = null!;

    [ForeignKey("DeliveryDriverId")]
    public DeliveryDriver Driver { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
