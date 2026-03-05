using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Reviews")]
public class Review : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Content { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsVisible { get; set; } = true;

    [MaxLength(2000)]
    public string? AdminReply { get; set; }

    public DateTime? AdminRepliedAt { get; set; }

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
