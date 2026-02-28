using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_RefreshTokens")]
public class RefreshToken : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
