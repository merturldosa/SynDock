using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_GamePlayers")]
public class GamePlayer : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int GameRoomId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int Rank { get; set; } // Final ranking (1st, 2nd, etc.)
    public bool IsReady { get; set; }
    public bool HasBet { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal WinAmount { get; set; } // Amount won
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("GameRoomId")] public virtual GameRoom GameRoom { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
}
