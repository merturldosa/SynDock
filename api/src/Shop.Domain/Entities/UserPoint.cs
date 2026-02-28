using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_UserPoints")]
public class UserPoint : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Balance { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
