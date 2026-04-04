using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DeliveryAssignments")]
public class DeliveryAssignment : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int OrderId { get; set; }

    public int? DeliveryDriverId { get; set; }

    public int? DeliveryOptionId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = nameof(DeliveryAssignmentStatus.Pending);

    public DateTime? OfferedAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? InTransitAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    public double? AcceptLatitude { get; set; }

    public double? AcceptLongitude { get; set; }

    [MaxLength(500)]
    public string? DeliveryPhotoUrl { get; set; }

    [MaxLength(500)]
    public string? DeliveryNote { get; set; }

    public DateTime? EstimatedDeliveryAt { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("DeliveryDriverId")]
    public DeliveryDriver? Driver { get; set; }

    [ForeignKey("DeliveryOptionId")]
    public DeliveryOption? DeliveryOption { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
