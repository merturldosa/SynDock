using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Coupons")]
public class Coupon : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(20)]
    public string DiscountType { get; set; } = nameof(CouponType.Fixed);

    [Column(TypeName = "decimal(18,0)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal MinOrderAmount { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? MaxDiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int MaxUsageCount { get; set; }

    public int CurrentUsageCount { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();
}
