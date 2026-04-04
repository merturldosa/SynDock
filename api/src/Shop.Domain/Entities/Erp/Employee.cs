using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Employees")]
public class Employee : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int? UserId { get; set; } // Linked platform user (optional)

    [Required, MaxLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(50)]
    public string Department { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Position { get; set; } = string.Empty;

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, OnLeave, Terminated

    [Column(TypeName = "decimal(18,0)")]
    public decimal BaseSalary { get; set; }

    [MaxLength(20)]
    public string PayType { get; set; } = "Monthly"; // Monthly, Hourly, Contract

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
}
