using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_LeadScores")]
public class LeadScore : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int UserId { get; set; }
    public int TotalScore { get; set; }
    public int PurchaseScore { get; set; } // Based on order history
    public int EngagementScore { get; set; } // Based on site activity
    public int RecencyScore { get; set; } // Based on last activity
    public int FrequencyScore { get; set; } // Based on visit frequency
    [Required, MaxLength(10)] public string Grade { get; set; } = "C"; // A, B, C, D, F
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "jsonb")] public string? ScoreBreakdownJson { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
}
