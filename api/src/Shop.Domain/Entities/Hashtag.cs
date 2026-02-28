using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Hashtags")]
public class Hashtag : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Tag { get; set; } = string.Empty;

    public int PostCount { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
}
