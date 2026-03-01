using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_TenantPlans")]
public class TenantPlan : BaseEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(20)]
    public string PlanType { get; set; } = "Free";

    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyPrice { get; set; }

    [Required]
    [MaxLength(20)]
    public string BillingStatus { get; set; } = "Trial";

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? NextBillingAt { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
