using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_SalesPipelines")]
public class SalesPipeline : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int? UserId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Stage { get; set; } = "Lead"; // Lead, Contacted, Qualified, Proposal, Negotiation, Won, Lost
    [Column(TypeName = "decimal(18,0)")] public decimal ExpectedValue { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Probability { get; set; } // 0~100
    public DateTime? ExpectedCloseDate { get; set; }
    [MaxLength(50)] public string? AssignedTo { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime? WonAt { get; set; }
    public DateTime? LostAt { get; set; }
    [MaxLength(200)] public string? LostReason { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User? User { get; set; }
}
