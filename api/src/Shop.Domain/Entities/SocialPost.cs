using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_SocialPosts")]
public class SocialPost : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int? ProductId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Platform { get; set; } = string.Empty; // Instagram, Facebook, Twitter

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Posted, Failed

    [MaxLength(2200)]
    public string Caption { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? PostUrl { get; set; }

    [MaxLength(200)]
    public string? ExternalPostId { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime? PostedAt { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
