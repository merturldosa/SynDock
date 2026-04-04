using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CustomerSegments")]
public class CustomerSegment : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string Type { get; set; } = "Manual"; // Manual, Dynamic, AI

    [Column(TypeName = "jsonb")]
    public string? RulesJson { get; set; } // Dynamic rules: {"minOrders": 3, "minSpent": 100000, "lastOrderDays": 90}

    public int MemberCount { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastCalculatedAt { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<CustomerTagAssignment> TagAssignments { get; set; } = new List<CustomerTagAssignment>();
}
