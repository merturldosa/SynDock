using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_LiturgicalSeasons")]
public class LiturgicalSeason : BaseEntity
{
    public int Year { get; set; }

    [Required]
    [MaxLength(50)]
    public string SeasonName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string LiturgicalColor { get; set; } = string.Empty;
}
