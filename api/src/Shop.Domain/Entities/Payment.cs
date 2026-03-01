using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Payments")]
public class Payment : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = nameof(PaymentStatus.Pending);

    [Column(TypeName = "decimal(18,0)")]
    public decimal Amount { get; set; }

    [MaxLength(100)]
    public string? TransactionId { get; set; }

    public DateTime? PaidAt { get; set; }

    [MaxLength(200)]
    public string? PaymentKey { get; set; }

    [MaxLength(50)]
    public string? ProviderName { get; set; }

    [MaxLength(500)]
    public string? FailReason { get; set; }

    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    // Navigation
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
