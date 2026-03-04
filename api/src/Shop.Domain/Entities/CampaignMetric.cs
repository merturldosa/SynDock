using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_CampaignMetrics")]
public class CampaignMetric : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int CampaignId { get; set; }

    public int? VariantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string EventType { get; set; } = string.Empty; // Sent, Opened, Clicked, Converted, Bounced, Unsubscribed

    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    [MaxLength(100)]
    public string? UserAgent { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public EmailCampaign? Campaign { get; set; }

    [ForeignKey(nameof(VariantId))]
    public CampaignVariant? Variant { get; set; }
}
