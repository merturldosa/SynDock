using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_AccountingPeriods")]
public class AccountingPeriod : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(10)]
    public string Period { get; set; } = string.Empty; // YYYY-MM

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Open"; // Open, Closed

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDebits { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCredits { get; set; }

    public bool IsBalanced { get; set; }

    public int EntriesCount { get; set; }

    public DateTime? ClosedAt { get; set; }

    [MaxLength(50)]
    public string? ClosedBy { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;
}
