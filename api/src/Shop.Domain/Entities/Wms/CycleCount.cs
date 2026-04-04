using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CycleCounts")]
public class CycleCount : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string CountNumber { get; set; } = string.Empty;
    public int? ZoneId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Approved
    [Required, MaxLength(20)] public string CountType { get; set; } = "Full"; // Full, ABC_A, ABC_B, ABC_C, Spot
    public int? AssignedUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public int DiscrepancyItems { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal AccuracyPercent { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("ZoneId")] public virtual WarehouseZone? Zone { get; set; }
    public virtual ICollection<CycleCountItem> Items { get; set; } = new List<CycleCountItem>();
}
