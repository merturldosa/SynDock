using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Carts")]
public class Cart : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
