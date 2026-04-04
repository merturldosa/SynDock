using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_MemberGrades")]
public class MemberGrade : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(20)] public string Grade { get; set; } = "Bronze"; // Bronze, Silver, Gold, Platinum, Diamond, VIP
    public int GradePoints { get; set; } // Total accumulated points for grade calculation
    [Column(TypeName = "decimal(18,0)")] public decimal TotalPurchaseAmount { get; set; }
    public int TotalOrders { get; set; }
    public int TotalReviews { get; set; }
    public int TotalReferrals { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal SdtBalance { get; set; }
    public int GiftsGiven { get; set; }
    public int GiftsReceived { get; set; }
    [Column(TypeName = "decimal(5,1)")] public decimal BonusRate { get; set; } // SDT earning bonus rate (%)
    public DateTime? GradeUpdatedAt { get; set; }
    public DateTime? NextReviewAt { get; set; } // Next grade recalculation date
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
}
