using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_AccountLockouts")]
public class AccountLockout : BaseEntity
{
    public int UserId { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    public int FailedAttempts { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Locked"; // Locked, Unlocked
    [MaxLength(500)] public string? Reason { get; set; }
    public DateTime? LockedUntil { get; set; } // null = until manual unlock
    public DateTime? UnlockedAt { get; set; }
    [MaxLength(100)] public string? UnlockedBy { get; set; }
    [MaxLength(50)] public string? LastAttemptIp { get; set; }
}
