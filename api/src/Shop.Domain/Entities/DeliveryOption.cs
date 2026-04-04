using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_DeliveryOptions")]
public class DeliveryOption : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(20)]
    public string DeliveryType { get; set; } = nameof(Enums.DeliveryType.Standard);

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal AdditionalFee { get; set; }

    public int MaxDeliveryMinutes { get; set; }

    public double MaxDistanceKm { get; set; }

    [MaxLength(5)]
    public string? AvailableFrom { get; set; }

    [MaxLength(5)]
    public string? AvailableTo { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
