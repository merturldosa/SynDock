using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_CostAnalyses")]
public class CostAnalysis : BaseEntity, ITenantEntity
{
    [Required]
    public int TenantId { get; set; }

    public int? ProductId { get; set; }

    [Required, MaxLength(10)]
    public string AnalysisPeriod { get; set; } = string.Empty; // e.g. "2026-03"

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OverheadCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossProfit { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal GrossMarginPercent { get; set; }

    public int UnitsSold { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPerUnit { get; set; }

    [ForeignKey("TenantId")]
    public virtual Tenant Tenant { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
