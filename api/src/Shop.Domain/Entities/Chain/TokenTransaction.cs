using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_TokenTransactions")]
public class TokenTransaction : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToUserId { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal Amount { get; set; }
    [Required, MaxLength(20)] public string TransactionType { get; set; } = string.Empty; // Earn, Spend, Transfer, Reward, Refund
    [MaxLength(200)] public string? Description { get; set; }
    [MaxLength(50)] public string? ReferenceType { get; set; } // Order, Review, Referral, Promotion
    public int? ReferenceId { get; set; }
    [MaxLength(66)] public string? OnChainTxHash { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
}
