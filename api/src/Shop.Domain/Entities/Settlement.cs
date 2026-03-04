using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

/// <summary>
/// 정산 배치 (prd.txt #11)
/// 주별 특정 요일에 쇼핑몰 계좌에 자동 송금
/// </summary>
[Table("SP_Settlements")]
public class Settlement : BaseEntity
{
    public int TenantId { get; set; }

    /// <summary>정산 기간 시작일</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>정산 기간 종료일</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>포함된 주문 건수</summary>
    public int OrderCount { get; set; }

    /// <summary>총 주문 금액</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalOrderAmount { get; set; }

    /// <summary>총 수수료 (우리 회사 수익)</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalCommission { get; set; }

    /// <summary>총 정산 금액 (쇼핑몰에 지급할 금액)</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalSettlementAmount { get; set; }

    /// <summary>Pending → Ready → Processing → Completed → Failed</summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>정산 수취 은행명 (정산 시점 스냅샷)</summary>
    [MaxLength(50)]
    public string? BankName { get; set; }

    /// <summary>정산 수취 계좌번호 (정산 시점 스냅샷)</summary>
    [MaxLength(50)]
    public string? BankAccount { get; set; }

    /// <summary>송금 거래 ID</summary>
    [MaxLength(100)]
    public string? TransactionId { get; set; }

    /// <summary>정산 완료 시각</summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>처리자</summary>
    [MaxLength(50)]
    public string? SettledBy { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    public ICollection<Commission> Commissions { get; set; } = new List<Commission>();
}
