using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Posts")]
public class Post : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Post type from ConfigJson.postTypes (e.g., "review", "daily", "recipe")
    /// </summary>
    [MaxLength(50)]
    public string PostType { get; set; } = "general";

    /// <summary>
    /// Optional product tag
    /// </summary>
    public int? ProductId { get; set; }

    public int ViewCount { get; set; }

    public int ReactionCount { get; set; }

    public int CommentCount { get; set; }

    public bool IsVisible { get; set; } = true;

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
}
