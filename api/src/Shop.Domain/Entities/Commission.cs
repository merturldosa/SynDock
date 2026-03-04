using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

/// <summary>
/// 주문별 수수료 내역 (prd.txt #11)
/// 주문 결제 완료 시 자동 계산
/// </summary>
[Table("SP_Commissions")]
public class Commission : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int OrderId { get; set; }

    /// <summary>주문 총 결제 금액 (수수료 산정 기준)</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal OrderAmount { get; set; }

    /// <summary>적용된 수수료율 (%)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionRate { get; set; }

    /// <summary>수수료 금액 = OrderAmount * CommissionRate / 100</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal CommissionAmount { get; set; }

    /// <summary>쇼핑몰 정산 금액 = OrderAmount - CommissionAmount</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal SettlementAmount { get; set; }

    /// <summary>Pending → Settled → Paid</summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public int? SettlementId { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;

    [ForeignKey("SettlementId")]
    public Settlement? Settlement { get; set; }
}
