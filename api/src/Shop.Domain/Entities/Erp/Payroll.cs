using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Payrolls")]
public class Payroll : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int EmployeeId { get; set; }

    [Required, MaxLength(10)]
    public string PayPeriod { get; set; } = string.Empty; // e.g. "2026-03"

    [Column(TypeName = "decimal(18,0)")]
    public decimal BasePay { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal OvertimePay { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Bonus { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Deductions { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal InsuranceAmount { get; set; } // 4대보험

    [Column(TypeName = "decimal(18,0)")]
    public decimal NetPay { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Approved, Paid

    public DateTime? PaidAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    public virtual Employee Employee { get; set; } = null!;
}
