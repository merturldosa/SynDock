using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PostHashtags")]
public class PostHashtag : BaseEntity
{
    public int PostId { get; set; }

    public int HashtagId { get; set; }

    // Navigation
    [ForeignKey("PostId")]
    public Post Post { get; set; } = null!;

    [ForeignKey("HashtagId")]
    public Hashtag Hashtag { get; set; } = null!;
}
