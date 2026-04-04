using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DeliveryDrivers")]
public class DeliveryDriver : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = nameof(DriverStatus.Offline);

    [Required]
    [MaxLength(20)]
    public string VehicleType { get; set; } = nameof(Enums.VehicleType.Motorcycle);

    [MaxLength(20)]
    public string? LicensePlate { get; set; }

    [MaxLength(50)]
    public string? LicenseNumber { get; set; }

    public double? LastLatitude { get; set; }

    public double? LastLongitude { get; set; }

    public DateTime? LastLocationAt { get; set; }

    public bool IsApproved { get; set; }

    public bool IsActive { get; set; } = true;

    public double AverageRating { get; set; }

    public int TotalDeliveries { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<DeliveryZoneDriver> ZoneDrivers { get; set; } = new List<DeliveryZoneDriver>();
}
