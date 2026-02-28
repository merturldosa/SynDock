using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PostComments")]
public class PostComment : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Parent comment ID for 2-depth threading (null = top-level)
    /// </summary>
    public int? ParentId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public bool IsVisible { get; set; } = true;

    // Navigation
    [ForeignKey("PostId")]
    public Post Post { get; set; } = null!;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("ParentId")]
    public PostComment? Parent { get; set; }

    public ICollection<PostComment> Replies { get; set; } = new List<PostComment>();

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
