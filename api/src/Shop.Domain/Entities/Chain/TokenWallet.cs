using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_TokenWallets")]
public class TokenWallet : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int UserId { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal Balance { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal TotalEarned { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal TotalSpent { get; set; }
    [MaxLength(42)] public string? OnChainAddress { get; set; } // Polygon wallet address
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
}
