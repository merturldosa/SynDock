using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CustomerTagAssignments")]
public class CustomerTagAssignment : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int UserId { get; set; }
    public int? CustomerTagId { get; set; }
    public int? CustomerSegmentId { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("CustomerTagId")]
    public virtual CustomerTag? Tag { get; set; }

    [ForeignKey("CustomerSegmentId")]
    public virtual CustomerSegment? Segment { get; set; }
}
