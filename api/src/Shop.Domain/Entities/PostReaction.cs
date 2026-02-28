using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PostReactions")]
public class PostReaction : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Reaction type from ConfigJson.reactionTypes (e.g., "like", "pray", "recommend")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ReactionType { get; set; } = "like";

    // Navigation
    [ForeignKey("PostId")]
    public Post Post { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
