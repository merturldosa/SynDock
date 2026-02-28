using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_UserCoupons")]
public class UserCoupon : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int UserId { get; set; }

    public int CouponId { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public int? UsedOrderId { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("CouponId")]
    public Coupon Coupon { get; set; } = null!;

    [ForeignKey("UsedOrderId")]
    public Order? UsedOrder { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;
}
