using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Products")]
public class Product : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Slug { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? SalePrice { get; set; }

    [Required]
    [MaxLength(20)]
    public string PriceType { get; set; } = nameof(Enums.PriceType.Fixed);

    [MaxLength(200)]
    public string? Specification { get; set; }

    public int CategoryId { get; set; }

    [MaxLength(20)]
    public string? SourceId { get; set; }

    [MaxLength(50)]
    public string? SourceSubCategory { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFeatured { get; set; }

    public bool IsNew { get; set; }

    public int ViewCount { get; set; }

    [Column(TypeName = "jsonb")]
    public string? CustomFieldsJson { get; set; }

    // Navigation
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
