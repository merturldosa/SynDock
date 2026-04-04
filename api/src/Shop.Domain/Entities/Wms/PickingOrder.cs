using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_PickingOrders")]
public class PickingOrder : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(50)]
    public string PickingNumber { get; set; } = string.Empty;

    public int OrderId { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled

    public int? AssignedUserId { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalItems { get; set; }
    public int PickedItems { get; set; }

    [MaxLength(20)]
    public string Priority { get; set; } = "Normal"; // Urgent, High, Normal, Low

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("AssignedUserId")]
    public virtual User? AssignedUser { get; set; }

    public virtual ICollection<PickingItem> Items { get; set; } = new List<PickingItem>();
}
