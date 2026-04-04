using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_AccountEntries")]
public class AccountEntry : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(50)]
    public string EntryNumber { get; set; } = string.Empty; // 전표번호

    public int ChartOfAccountId { get; set; }

    public DateTime EntryDate { get; set; }

    [Required, MaxLength(10)]
    public string EntryType { get; set; } = string.Empty; // Debit, Credit

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ReferenceType { get; set; } // Order, Settlement, Payroll, Manual

    public int? ReferenceId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Posted, Reversed

    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("ChartOfAccountId")]
    public virtual ChartOfAccount Account { get; set; } = null!;
}
