namespace Shop.Application.Common.Interfaces;

public interface IAiForecastInsightService
{
    Task<AiInsight?> GenerateInsightAsync(
        string productName,
        string categoryName,
        List<DailyDemand> historicalDemand,
        List<DailyDemand> forecast,
        int currentStock,
        CancellationToken ct = default);

    Task<AiInsight?> GenerateCategoryInsightAsync(
        string categoryName,
        int productCount,
        double totalAverageDailyDemand,
        int totalStock,
        int minDaysUntilStockout,
        CancellationToken ct = default);
}
