using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_RoomAmenityLogs")]
public class RoomAmenityLog : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int RoomId { get; set; }
    public int? CleaningTaskId { get; set; }
    [Required, MaxLength(100)] public string ItemName { get; set; } = string.Empty; // Towel, Shampoo, Soap, Toothbrush, SlipperSet, MinibarItem
    public int Quantity { get; set; } = 1;
    [Required, MaxLength(20)] public string ActionType { get; set; } = "Restocked"; // Restocked, Replaced, Missing, Damaged
    [MaxLength(200)] public string? Notes { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("RoomId")] public virtual Room Room { get; set; } = null!;
    [ForeignKey("CleaningTaskId")] public virtual CleaningTask? CleaningTask { get; set; }
}
