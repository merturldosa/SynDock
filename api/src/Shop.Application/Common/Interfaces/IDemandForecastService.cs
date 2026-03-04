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
    AiInsight? AiInsight = null,
    double? TrendSlope = null,
    string? TrendDirection = null,
    double? SeasonalityStrength = null,
    string? ForecastMethod = null);

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

public record ForecastAccuracyResult(
    int ProductId,
    string ProductName,
    double Mape,
    double Mae,
    int ForecastCount,
    List<AccuracyDataPoint> DataPoints);

public record AccuracyDataPoint(
    DateTime TargetDate,
    double Predicted,
    double Actual,
    double PercentageError);

public record AutoPurchaseOrderResult(
    bool Success,
    string? MesOrderId,
    int ProductCount,
    int TotalQuantity,
    string? ErrorMessage);

public record BatchAiInsightResult(
    List<ProductAiSummary> Products,
    string OverallSummary,
    double AverageConfidence);

public record ProductAiSummary(
    int ProductId,
    string ProductName,
    string TrendDirection,
    double SeasonalityStrength,
    string KeyInsight);

public interface IDemandForecastService
{
    Task<ForecastResult> ForecastAsync(int productId, int forecastDays = 30, CancellationToken ct = default);
    Task<List<ForecastResult>> GetLowStockForecastsAsync(int daysThreshold = 14, CancellationToken ct = default);
    Task<ForecastResult> ForecastWithAiAsync(int productId, int forecastDays = 30, CancellationToken ct = default);
    Task<CategoryForecastResult> ForecastCategoryAsync(int categoryId, int forecastDays = 30, CancellationToken ct = default);
    Task<List<CategoryForecastResult>> GetAllCategoryForecastsAsync(int forecastDays = 30, CancellationToken ct = default);
    Task<List<PurchaseRecommendation>> GetPurchaseRecommendationsAsync(int daysThreshold = 14, CancellationToken ct = default);

    // Sprint 5: Holt-Winters & accuracy tracking
    Task<ForecastResult> ForecastHoltWintersAsync(int productId, int forecastDays = 30, CancellationToken ct = default);
    Task RecordForecastAsync(int productId, CancellationToken ct = default);
    Task UpdateActualDemandAsync(CancellationToken ct = default);
    Task<ForecastAccuracyResult?> GetAccuracyAsync(int productId, CancellationToken ct = default);
    Task<List<ForecastAccuracyResult>> GetAllAccuraciesAsync(CancellationToken ct = default);
    Task<AutoPurchaseOrderResult> CreateAutoPurchaseOrderAsync(List<int> productIds, CancellationToken ct = default);
    Task<BatchAiInsightResult> GetBatchAiInsightsAsync(CancellationToken ct = default);
}
