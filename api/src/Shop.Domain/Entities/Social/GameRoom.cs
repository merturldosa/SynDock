using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_GameRooms")]
public class GameRoom : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    [Required, MaxLength(50)] public string RoomCode { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string GameType { get; set; } = string.Empty; // WordChain, TruthGame, NonsenseQuiz, HiddenPicture, JigsawPuzzle, SpeedQuiz
    [Required, MaxLength(10)] public string Mode { get; set; } = "1v1"; // 1v1, NvN
    [Required, MaxLength(20)] public string Status { get; set; } = "Waiting"; // Waiting, Playing, Finished, Cancelled
    public int HostUserId { get; set; }
    public int MaxPlayers { get; set; } = 2;
    public int CurrentRound { get; set; }
    public int TotalRounds { get; set; } = 5;
    // Betting
    [Required, MaxLength(20)] public string BetType { get; set; } = "None"; // None, SDT, Points, Coupon
    [Column(TypeName = "decimal(18,4)")] public decimal BetAmount { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal TotalPot { get; set; } // Total betting pool
    // Game data
    [Column(TypeName = "jsonb")] public string? GameDataJson { get; set; } // Questions, answers, current state
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("HostUserId")] public virtual User Host { get; set; } = null!;
    public virtual ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
}
