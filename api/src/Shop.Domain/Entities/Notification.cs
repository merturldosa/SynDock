using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Notifications")]
public class Notification : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = nameof(NotificationType.System);

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public int? ReferenceId { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
