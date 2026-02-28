using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_ProductVariants")]
public class ProductVariant : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Sku { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? Price { get; set; }

    public int Stock { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
