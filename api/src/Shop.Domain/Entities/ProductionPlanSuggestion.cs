using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shop.Domain.Interfaces;
using SynDock.Core.Entities;

namespace Shop.Domain.Entities;

[Table("SP_ProductionPlanSuggestions")]
public class ProductionPlanSuggestion : BaseEntity, ITenantEntity
{
    public int TenantId { get; set; }

    public int ProductId { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int CurrentStock { get; set; }

    public double AverageDailySales { get; set; }

    public int EstimatedDaysUntilStockout { get; set; }

    public int SuggestedQuantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string Urgency { get; set; } = "Normal"; // Critical, High, Normal, Low

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Forwarded

    [MaxLength(1000)]
    public string? AiReason { get; set; }

    [MaxLength(500)]
    public string? TrendAnalysis { get; set; }

    public double? SeasonalityFactor { get; set; }

    public double? ConfidenceScore { get; set; }

    [MaxLength(100)]
    public string? MesOrderId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
