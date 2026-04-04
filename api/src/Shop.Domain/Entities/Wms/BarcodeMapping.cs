using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_BarcodeMappings")]
public class BarcodeMapping : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(200)]
    public string Barcode { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string BarcodeType { get; set; } = "EAN13"; // EAN13, UPC, QR, Code128, Custom

    [Required, MaxLength(20)]
    public string EntityType { get; set; } = "Product"; // Product, Variant, Location, Package

    public int EntityId { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;
}
