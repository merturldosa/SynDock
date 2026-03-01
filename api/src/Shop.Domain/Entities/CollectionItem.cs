using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_CollectionItems")]
public class CollectionItem : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int CollectionId { get; set; }

    public int ProductId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CollectionId")]
    public Collection Collection { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
