using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CleaningTasks")]
public class CleaningTask : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int RoomId { get; set; }
    public int? BookingId { get; set; }
    [Required, MaxLength(20)] public string TaskType { get; set; } = "Checkout"; // Checkout, StayOver, DeepClean, Maintenance
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Inspected, Issue
    [Required, MaxLength(20)] public string Priority { get; set; } = "Normal"; // Urgent, High, Normal, Low
    public int? AssignedToUserId { get; set; }
    [MaxLength(100)] public string? AssignedToName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? InspectedAt { get; set; }
    public int? InspectedByUserId { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(500)] public string? IssueDescription { get; set; }
    [Column(TypeName = "jsonb")] public string? ChecklistJson { get; set; } // {"bedMade":true,"bathroom":true,"vacuum":true,"minibar":true,"towels":true}
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("RoomId")] public virtual Room Room { get; set; } = null!;
    [ForeignKey("BookingId")] public virtual Booking? Booking { get; set; }
    [ForeignKey("AssignedToUserId")] public virtual User? AssignedTo { get; set; }
}
