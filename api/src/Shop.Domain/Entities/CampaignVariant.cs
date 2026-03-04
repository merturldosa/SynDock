using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_CampaignVariants")]
public class CampaignVariant : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int CampaignId { get; set; }

    [Required]
    [MaxLength(10)]
    public string VariantName { get; set; } = "A"; // A, B

    [Required]
    [MaxLength(200)]
    public string SubjectLine { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int TrafficPercent { get; set; } = 50; // % of target audience

    public int SentCount { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public int ConversionCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }

    public bool IsWinner { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public EmailCampaign? Campaign { get; set; }
}
