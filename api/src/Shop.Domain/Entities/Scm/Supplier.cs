using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Suppliers")]
public class Supplier : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(200)] public string? ContactName { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(50)] public string? BusinessNumber { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Active"; // Active, Inactive, Blacklisted
    [Required, MaxLength(20)] public string Grade { get; set; } = "B"; // S, A, B, C, D
    public int LeadTimeDays { get; set; } = 7;
    [Column(TypeName = "decimal(5,2)")] public decimal DefectRate { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal OnTimeDeliveryRate { get; set; } = 100;
    public int TotalOrders { get; set; }
    [Column(TypeName = "decimal(18,0)")] public decimal TotalAmount { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [Column(TypeName = "jsonb")] public string? MetadataJson { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<ProcurementOrder> ProcurementOrders { get; set; } = new List<ProcurementOrder>();
}
