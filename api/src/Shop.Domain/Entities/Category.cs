using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Categories")]
public class Category : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(10)]
    public string? Icon { get; set; }

    [MaxLength(10)]
    public string? SourceCode { get; set; }

    public int? ParentId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("ParentId")]
    public Category? Parent { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Category> Children { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
