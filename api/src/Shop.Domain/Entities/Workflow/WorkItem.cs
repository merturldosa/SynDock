using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_WorkItems")]
public class WorkItem : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(30)] public string Module { get; set; } = string.Empty; // Shop, WMS, MES, CRM, ERP, SCM, PMS
    [Required, MaxLength(50)] public string WorkType { get; set; } = string.Empty; // OrderConfirm, PickingAssign, QualityCheck, InvoiceApprove, PayrollApprove, etc.
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled, AutoCompleted
    [Required, MaxLength(20)] public string Priority { get; set; } = "Normal"; // Urgent, High, Normal, Low
    [MaxLength(50)] public string? Department { get; set; } // Sales, Warehouse, Accounting, CS, HR, Production, Marketing
    public int? AssignedToUserId { get; set; }
    [MaxLength(100)] public string? AssignedToName { get; set; }
    [MaxLength(50)] public string? ReferenceType { get; set; } // Order, PickingOrder, AccountEntry, Payroll, etc.
    public int? ReferenceId { get; set; }
    [MaxLength(500)] public string? ActionUrl { get; set; } // Deep link to the relevant page
    public DateTime? DueDate { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    [MaxLength(200)] public string? CompletedBy { get; set; }
    public bool IsAutoCreated { get; set; } // Created by AI/system
    public bool CanAutoComplete { get; set; } // AI can auto-complete this
    [MaxLength(500)] public string? AiSuggestion { get; set; } // AI's recommended action
    [Column(TypeName = "jsonb")] public string? MetadataJson { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("AssignedToUserId")] public virtual User? AssignedTo { get; set; }
}
