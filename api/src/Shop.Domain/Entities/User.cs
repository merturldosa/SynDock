using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Users")]
public class User : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = nameof(UserRole.Member);

    public bool IsActive { get; set; } = true;

    public bool EmailVerified { get; set; } = false;

    [MaxLength(100)]
    public string? EmailVerificationToken { get; set; }

    [MaxLength(100)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public DateTime? Birthday { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool TwoFactorEnabled { get; set; } = false;

    [MaxLength(100)]
    public string? TwoFactorSecret { get; set; }

    [Column(TypeName = "jsonb")]
    public string? TwoFactorBackupCodes { get; set; }

    [Column(TypeName = "jsonb")]
    public string? CustomFieldsJson { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
