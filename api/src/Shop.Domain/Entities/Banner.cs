using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Banners")]
public class Banner : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    [MaxLength(50)]
    public string DisplayType { get; set; } = "Banner"; // Banner, Popup

    [MaxLength(100)]
    public string? PageTarget { get; set; } // home, products, category

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
