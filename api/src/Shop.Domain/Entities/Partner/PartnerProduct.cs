using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PartnerProducts")]
public class PartnerProduct : BaseEntity
{
    public int ApiPartnerId { get; set; }
    [Required, MaxLength(100)] public string ExternalProductId { get; set; } = string.Empty; // Partner's product ID
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal? SalePrice { get; set; }
    [MaxLength(100)] public string? Category { get; set; }
    [MaxLength(500)] public string? ImageUrl { get; set; }
    [MaxLength(500)] public string? ProductUrl { get; set; } // Link back to partner's site
    public int Stock { get; set; }
    [MaxLength(50)] public string? Sku { get; set; }
    [MaxLength(50)] public string? Brand { get; set; }
    [Required, MaxLength(20)] public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected, Suspended
    [MaxLength(500)] public string? RejectionReason { get; set; }
    public int? SynDockProductId { get; set; } // Linked SynDock product after approval
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(50)] public string? ApprovedBy { get; set; }
    [Column(TypeName = "jsonb")] public string? AttributesJson { get; set; } // Flexible product attributes
    [ForeignKey("ApiPartnerId")] public virtual ApiPartner Partner { get; set; } = null!;
}
