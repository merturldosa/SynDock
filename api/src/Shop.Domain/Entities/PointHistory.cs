using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PointHistories")]
public class PointHistory : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = nameof(PointTransactionType.Earned);

    [MaxLength(200)]
    public string? Description { get; set; }

    public int? OrderId { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
