using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_VocEntries")]
public class VocEntry : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int? UserId { get; set; }
    [Required, MaxLength(20)] public string Source { get; set; } = string.Empty; // Review, QnA, CsTicket, Survey, Social
    public int? SourceId { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Sentiment { get; set; } = "Neutral"; // Positive, Neutral, Negative
    [Column(TypeName = "decimal(3,2)")] public decimal SentimentScore { get; set; } // -1.0 ~ 1.0
    [MaxLength(100)] public string? TopicCategory { get; set; } // Product Quality, Shipping, Service, Price
    [MaxLength(200)] public string? Keywords { get; set; }
    public bool IsProcessed { get; set; }
    [MaxLength(500)] public string? ActionTaken { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User? User { get; set; }
}
