using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_AutoReorderRules")]
public class AutoReorderRule : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>재고가 이 수량 이하로 떨어지면 자동 발주</summary>
    public int ReorderThreshold { get; set; } = 10;

    /// <summary>자동 발주 수량 (0이면 수요예측 기반 자동 계산)</summary>
    public int ReorderQuantity { get; set; }

    /// <summary>최대 재고 수준 (자동 발주 시 이 수준까지 채움, 0이면 무시)</summary>
    public int MaxStockLevel { get; set; }

    /// <summary>자동 발주 활성화 여부</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>MES 자동 전송 여부 (false면 PO만 생성, true면 MES까지 자동 전송)</summary>
    public bool AutoForwardToMes { get; set; } = true;

    /// <summary>마지막 자동 발주 일시</summary>
    public DateTime? LastTriggeredAt { get; set; }

    /// <summary>최소 발주 간격 (시간) - 너무 빈번한 발주 방지</summary>
    public int MinIntervalHours { get; set; } = 24;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
