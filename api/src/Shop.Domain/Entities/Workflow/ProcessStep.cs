using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_ProcessSteps")]
public class ProcessStep : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string ProcessType { get; set; } = string.Empty; // OrderFulfillment, Production, Procurement, Settlement
    [MaxLength(50)] public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    [Required, MaxLength(100)] public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Waiting"; // Waiting, Active, Completed, Skipped, Error
    public DateTime? CompletedAt { get; set; }
    [MaxLength(200)] public string? CompletedBy { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
}
