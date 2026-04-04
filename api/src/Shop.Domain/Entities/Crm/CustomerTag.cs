using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CustomerTags")]
public class CustomerTag : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6"; // hex color

    [MaxLength(200)]
    public string? Description { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<CustomerTagAssignment> Assignments { get; set; } = new List<CustomerTagAssignment>();
}
