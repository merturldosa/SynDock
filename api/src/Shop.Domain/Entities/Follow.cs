using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Follows")]
public class Follow : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// The user who is following
    /// </summary>
    public int FollowerId { get; set; }

    /// <summary>
    /// The user being followed
    /// </summary>
    public int FollowingId { get; set; }

    // Navigation
    [ForeignKey("FollowerId")]
    public User Follower { get; set; } = null!;

    [ForeignKey("FollowingId")]
    public User Following { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
