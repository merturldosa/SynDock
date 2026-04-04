using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_TenantApplications")]
public class TenantApplication : BaseEntity
{
    [Required, MaxLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string DesiredSlug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string BusinessType { get; set; } = string.Empty; // Retail, Food, Fashion, Religious, Manufacturing, Other

    [Required, MaxLength(20)]
    public string PlanTier { get; set; } = "Starter"; // Free, Starter, Pro, Enterprise

    [MaxLength(2000)]
    public string? BusinessDescription { get; set; }

    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    public bool NeedsMes { get; set; }
    public bool NeedsWms { get; set; }
    public bool NeedsErp { get; set; }
    public bool NeedsCrm { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Reviewing, Approved, Provisioning, Active, Rejected

    public int? ProvisionedTenantId { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ProvisionedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    [MaxLength(500)]
    public string? AdminNotes { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AdditionalInfoJson { get; set; }

    [ForeignKey("ProvisionedTenantId")]
    public virtual Tenant? ProvisionedTenant { get; set; }
}
