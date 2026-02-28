using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Pages")]
public class Page : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    public string? Content { get; set; }

    public bool IsPublished { get; set; } = true;

    public int SortOrder { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
