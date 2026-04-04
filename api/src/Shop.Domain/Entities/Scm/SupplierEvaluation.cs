using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SynDock.Core.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Domain.Entities;

[Table("SP_SupplierEvaluations")]
public class SupplierEvaluation : BaseEntity, ITenantEntity
{
    [Required] public int TenantId { get; set; }
    public int SupplierId { get; set; }
    [Required, MaxLength(10)] public string EvaluationPeriod { get; set; } = string.Empty; // e.g. "2026-Q1"
    public int QualityScore { get; set; } // 0-100
    public int DeliveryScore { get; set; } // 0-100
    public int PriceScore { get; set; } // 0-100
    public int ServiceScore { get; set; } // 0-100
    public int TotalScore { get; set; } // Average
    [Required, MaxLength(5)] public string Grade { get; set; } = "B"; // S(90+), A(80+), B(60+), C(40+), D(below)
    [MaxLength(500)] public string? Comments { get; set; }
    [ForeignKey("TenantId")] public virtual Tenant Tenant { get; set; } = null!;
    [ForeignKey("SupplierId")] public virtual Supplier Supplier { get; set; } = null!;
}
