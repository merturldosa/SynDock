using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ChartOfAccounts")]
public class ChartOfAccount : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(20)]
    public string AccountCode { get; set; } = string.Empty; // e.g. "1100", "4100"

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string AccountType { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense

    [MaxLength(20)]
    public string? ParentAccountCode { get; set; }

    public int Level { get; set; } = 1; // Hierarchy level

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<AccountEntry> Entries { get; set; } = new List<AccountEntry>();
}
