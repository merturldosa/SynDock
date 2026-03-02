namespace Shop.Application.Common.Interfaces;

public record DailyDemand(DateTime Date, int Quantity);

public record AiInsight(
    string TrendAnalysis,
    string SeasonalPatterns,
    string[] Recommendations,
    string? EventImpact,
    double ConfidenceScore);

public record ForecastResult(
    int ProductId,
    string ProductName,
    List<DailyDemand> HistoricalDemand,
    List<DailyDemand> ForecastedDemand,
    double AverageDailyDemand,
    int CurrentStock,
    int EstimatedDaysUntilStockout,
    AiInsight? AiInsight = null);

public record CategoryForecastResult(
    int CategoryId,
    string CategoryName,
    int ProductCount,
    int TotalStock,
    double TotalAverageDailyDemand,
    int MinDaysUntilStockout,
    List<ForecastResult> TopProducts,
    AiInsight? AiInsight = null);

public record PurchaseRecommendation(
    int ProductId,
    string ProductName,
    string? CategoryName,
    int CurrentStock,
    double AverageDailyDemand,
    int EstimatedDaysUntilStockout,
    int RecommendedOrderQuantity,
    string Urgency,
    string Reason);

public interface IDemandForecastService
{
    Task<ForecastResult> ForecastAsync(int productId, int forecastDays = 30, CancellationToken ct = default);
    Task<List<ForecastResult>> GetLowStockForecastsAsync(int daysThreshold = 14, CancellationToken ct = default);
    Task<ForecastResult> ForecastWithAiAsync(int productId, int forecastDays = 30, CancellationToken ct = default);
    Task<CategoryForecastResult> ForecastCategoryAsync(int categoryId, int forecastDays = 30, CancellationToken ct = default);
    Task<List<CategoryForecastResult>> GetAllCategoryForecastsAsync(int forecastDays = 30, CancellationToken ct = default);
    Task<List<PurchaseRecommendation>> GetPurchaseRecommendationsAsync(int daysThreshold = 14, CancellationToken ct = default);
}
