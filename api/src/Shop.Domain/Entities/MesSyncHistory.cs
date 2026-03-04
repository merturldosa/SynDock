using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_MesSyncHistories")]
public class MesSyncHistory : BaseEntity
{
    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Running"; // Running, Completed, Failed

    public int SuccessCount { get; set; }

    public int FailedCount { get; set; }

    public int SkippedCount { get; set; }

    public long ElapsedMs { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ErrorDetailsJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ConflictDetailsJson { get; set; }
}
