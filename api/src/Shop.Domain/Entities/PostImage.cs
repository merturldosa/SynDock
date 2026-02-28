using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_PostImages")]
public class PostImage : BaseEntity
{
    public int PostId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    [ForeignKey("PostId")]
    public Post Post { get; set; } = null!;
}
