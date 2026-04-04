using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Domain.Entities;

[Table("SP_AuditLogs")]
public class AuditLog
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    public int EntityId { get; set; }

    [Required]
    [MaxLength(10)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete

    [Column(TypeName = "jsonb")]
    public string? Changes { get; set; }

    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? Username { get; set; }

    public int? TenantId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
