using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_ForecastAccuracies")]
public class ForecastAccuracy : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public DateTime ForecastDate { get; set; }

    public DateTime TargetDate { get; set; }

    public double PredictedQuantity { get; set; }

    public double? ActualQuantity { get; set; }

    public double? AbsoluteError { get; set; }

    public double? PercentageError { get; set; }

    // Navigation
    [ForeignKey("ProductId")]
    public Product Product { get; set; } = null!;
}
