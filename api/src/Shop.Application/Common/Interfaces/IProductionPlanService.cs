namespace Shop.Application.Common.Interfaces;

public record ProductionSuggestionDto(
    int Id,
    int ProductId,
    string ProductName,
    int CurrentStock,
    double AverageDailySales,
    int EstimatedDaysUntilStockout,
    int SuggestedQuantity,
    string Urgency,
    string Status,
    string? AiReason,
    string? TrendAnalysis,
    double? SeasonalityFactor,
    double? ConfidenceScore,
    string? MesOrderId,
    DateTime? ApprovedAt,
    string? ApprovedBy,
    DateTime CreatedAt);

public interface IProductionPlanService
{
    Task<List<ProductionSuggestionDto>> GenerateSuggestionsAsync(CancellationToken ct = default);
    Task<ProductionSuggestionDto?> ApproveSuggestionAsync(int suggestionId, string approvedBy, CancellationToken ct = default);
    Task<bool> RejectSuggestionAsync(int suggestionId, string reason, CancellationToken ct = default);
    Task<string?> ForwardToMesAsync(int suggestionId, CancellationToken ct = default);
}
