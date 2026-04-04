using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_MarketplaceConnections")]
public class MarketplaceConnection : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(30)] public string MarketplaceCode { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string MarketplaceName { get; set; } = string.Empty;
    [MaxLength(500)] public string? ApiKey { get; set; }
    [MaxLength(500)] public string? ApiSecret { get; set; }
    [MaxLength(500)] public string? AccessToken { get; set; }
    [MaxLength(200)] public string? SellerId { get; set; }
    [MaxLength(500)] public string? StoreUrl { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Disconnected";
    public DateTime? LastSyncAt { get; set; }
    public int ProductsSynced { get; set; }
    public int OrdersSynced { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal PriceMarkupPercent { get; set; }
    [Column(TypeName = "jsonb")] public string? SettingsJson { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
}
