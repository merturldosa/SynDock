using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_BlockedIps")]
public class BlockedIp : BaseEntity
{
    [Required, MaxLength(50)] public string IpAddress { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string BlockType { get; set; } = "Temporary"; // Temporary, Permanent
    [MaxLength(500)] public string? Reason { get; set; }
    public int? SecurityEventId { get; set; }
    public DateTime? ExpiresAt { get; set; } // null = permanent
    public bool IsActive { get; set; } = true;
    [ForeignKey("SecurityEventId")] public virtual SecurityEvent? Event { get; set; }
}
