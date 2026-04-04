using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_BlockchainTransactions")]
public class BlockchainTransaction : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(20)] public string TransactionType { get; set; } = string.Empty; // Proof, SupplyChain, TokenTransfer, Escrow
    [MaxLength(100)] public string? ReferenceType { get; set; } // Order, Settlement, Product, Contract
    public int? ReferenceId { get; set; }
    [Required, MaxLength(66)] public string DataHash { get; set; } = string.Empty; // SHA-256 hash
    [MaxLength(66)] public string? OnChainTxHash { get; set; } // Polygon transaction hash
    [MaxLength(66)] public string? OnChainProofId { get; set; } // Proof ID from smart contract
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, Confirmed, Failed
    [MaxLength(20)] public string Network { get; set; } = "polygon_testnet";
    public int? BlockNumber { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    [Column(TypeName = "jsonb")] public string? MetadataJson { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
}
