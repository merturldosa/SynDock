using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

/// <summary>
/// 테넌트별 수수료 설정 (prd.txt #11)
/// 품목별 또는 일괄 수수료율 설정
/// </summary>
[Table("SP_CommissionSettings")]
public class CommissionSetting : BaseEntity
{
    public int TenantId { get; set; }

    /// <summary>특정 상품 ID (null이면 테넌트 전체 기본 수수료율)</summary>
    public int? ProductId { get; set; }

    /// <summary>특정 카테고리 ID (null이면 테넌트 전체 기본 수수료율)</summary>
    public int? CategoryId { get; set; }

    /// <summary>수수료율 (%, 0~100). 예: 5.0 = 5%</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionRate { get; set; } = 5.0m;

    /// <summary>정산 주기: Weekly, Biweekly, Monthly</summary>
    [Required]
    [MaxLength(20)]
    public string SettlementCycle { get; set; } = "Weekly";

    /// <summary>정산 요일 (0=Sunday ~ 6=Saturday). Weekly 시 사용</summary>
    public int SettlementDayOfWeek { get; set; } = 1; // Monday

    /// <summary>최소 정산 금액 (이하면 이월)</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal MinSettlementAmount { get; set; } = 10000m;

    /// <summary>정산 수취 은행명</summary>
    [MaxLength(50)]
    public string? BankName { get; set; }

    /// <summary>정산 수취 계좌번호</summary>
    [MaxLength(50)]
    public string? BankAccount { get; set; }

    /// <summary>정산 수취 예금주</summary>
    [MaxLength(50)]
    public string? BankHolder { get; set; }

    // Navigation
    [ForeignKey("TenantId")]
    public Tenant Tenant { get; set; } = null!;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }
}
