using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_Friendships")]
public class Friendship : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int RequesterId { get; set; }
    public int AddresseeId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Pending"; // Pending, Accepted, Blocked
    public DateTime? AcceptedAt { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("RequesterId")] public virtual User Requester { get; set; } = null!;
    [ForeignKey("AddresseeId")] public virtual User Addressee { get; set; } = null!;
}
