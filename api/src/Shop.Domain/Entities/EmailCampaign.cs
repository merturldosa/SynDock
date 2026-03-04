using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_EmailCampaigns")]
public class EmailCampaign : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Target { get; set; } = "all"; // all, new_users, vip, inactive

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Scheduled, Sending, Sent, Failed

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public int SentCount { get; set; }

    public int FailCount { get; set; }

    public int OpenCount { get; set; }

    public int ClickCount { get; set; }

    public int ConversionCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }

    public bool IsAbTest { get; set; }
}
