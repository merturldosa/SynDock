using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Attendances")]
public class Attendance : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime WorkDate { get; set; }

    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Present"; // Present, Absent, Late, HalfDay, Holiday, Leave

    [MaxLength(20)]
    public string? LeaveType { get; set; } // Annual, Sick, Personal, Maternity

    public double WorkHours { get; set; }
    public double OvertimeHours { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("EmployeeId")]
    public virtual Employee Employee { get; set; } = null!;
}
