using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_ProductImages")]
public class ProductImage : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    [MaxLength(200)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
