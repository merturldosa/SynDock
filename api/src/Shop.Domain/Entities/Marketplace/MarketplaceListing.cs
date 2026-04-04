using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_MarketplaceListings")]
public class MarketplaceListing : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int MarketplaceConnectionId { get; set; }
    public int ProductId { get; set; }
    [MaxLength(30)] public string MarketplaceCode { get; set; } = string.Empty;
    [MaxLength(100)] public string? ExternalProductId { get; set; }
    [MaxLength(500)] public string? ExternalUrl { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending";
    [Column(TypeName = "decimal(18,0)")] public decimal ListedPrice { get; set; }
    public int ListedStock { get; set; }
    [MaxLength(100)] public string? ExternalCategoryId { get; set; }
    [MaxLength(200)] public string? ExternalCategoryName { get; set; }
    public DateTime? ListedAt { get; set; }
    public DateTime? LastStockSyncAt { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("MarketplaceConnectionId")] public virtual MarketplaceConnection Connection { get; set; } = null!;
    [ForeignKey("ProductId")] public virtual Product Product { get; set; } = null!;
}
