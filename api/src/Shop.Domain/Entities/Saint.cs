using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Saints")]
public class Saint : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string KoreanName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LatinName { get; set; }

    [MaxLength(100)]
    public string? EnglishName { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DateTime? FeastDay { get; set; }

    [MaxLength(500)]
    public string? Patronage { get; set; }

    public bool IsActive { get; set; } = true;
}
