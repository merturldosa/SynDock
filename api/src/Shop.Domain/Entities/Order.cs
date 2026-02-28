using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Enums;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_Orders")]
public class Order : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = nameof(OrderStatus.Pending);

    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal ShippingFee { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal PointsUsed { get; set; }

    public int? CouponId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public int? ShippingAddressId { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("ShippingAddressId")]
    public Address? ShippingAddress { get; set; }

    [ForeignKey("CouponId")]
    public Coupon? Coupon { get; set; }

    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
