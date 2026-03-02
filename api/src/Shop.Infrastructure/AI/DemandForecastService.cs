using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.AI;

/// <summary>
/// Simple moving average demand forecast based on order history.
/// Uses 7-day and 30-day moving averages with day-of-week seasonality.
/// </summary>
public class DemandForecastService : IDemandForecastService
{
    private readonly IShopDbContext _db;
    private readonly IAiForecastInsightService _aiInsight;
    private readonly ITenantContext _tenantContext;

    public DemandForecastService(IShopDbContext db, IAiForecastInsightService aiInsight, ITenantContext tenantContext)
    {
        _db = db;
        _aiInsight = aiInsight;
        _tenantContext = tenantContext;
    }

    public async Task<ForecastResult> ForecastAsync(int productId, int forecastDays = 30, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
            return new ForecastResult(productId, "Unknown", [], [], 0, 0, int.MaxValue);

        // Get current stock
        var totalStock = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId && v.IsActive)
            .SumAsync(v => v.Stock, ct);

        // Get last 90 days of order data
        var since = DateTime.UtcNow.AddDays(-90);
        var orderData = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.ProductId == productId && oi.Order.CreatedAt >= since)
            .Select(oi => new { oi.Order.CreatedAt, oi.Quantity })
            .ToListAsync(ct);

        // Build daily demand history
        var dailyMap = new Dictionary<DateTime, int>();
        foreach (var item in orderData)
        {
            var date = item.CreatedAt.Date;
            dailyMap.TryGetValue(date, out var existing);
            dailyMap[date] = existing + item.Quantity;
        }

        var historicalDemand = new List<DailyDemand>();
        for (var d = since.Date; d <= DateTime.UtcNow.Date; d = d.AddDays(1))
        {
            dailyMap.TryGetValue(d, out var qty);
            historicalDemand.Add(new DailyDemand(d, qty));
        }

        // Calculate day-of-week seasonality factors
        var dowFactors = new double[7];
        var dowCounts = new int[7];
        foreach (var h in historicalDemand)
        {
            var dow = (int)h.Date.DayOfWeek;
            dowFactors[dow] += h.Quantity;
            dowCounts[dow]++;
        }
        for (var i = 0; i < 7; i++)
        {
            dowFactors[i] = dowCounts[i] > 0 ? dowFactors[i] / dowCounts[i] : 0;
        }
        var avgFactor = dowFactors.Average();
        if (avgFactor > 0)
        {
            for (var i = 0; i < 7; i++)
                dowFactors[i] /= avgFactor;
        }
        else
        {
            Array.Fill(dowFactors, 1.0);
        }

        // 7-day moving average
        var recent7 = historicalDemand.TakeLast(7).Average(h => (double)h.Quantity);
        var recent30 = historicalDemand.TakeLast(30).Average(h => (double)h.Quantity);
        var baseDemand = recent7 * 0.6 + recent30 * 0.4; // weighted blend

        // Generate forecast
        var forecast = new List<DailyDemand>();
        for (var i = 1; i <= forecastDays; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(i);
            var dow = (int)date.DayOfWeek;
            var predicted = (int)Math.Round(baseDemand * dowFactors[dow]);
            forecast.Add(new DailyDemand(date, Math.Max(0, predicted)));
        }

        var avgDaily = Math.Round(baseDemand, 1);
        var daysUntilStockout = baseDemand > 0 ? (int)Math.Floor(totalStock / baseDemand) : int.MaxValue;

        return new ForecastResult(
            productId,
            product.Name,
            historicalDemand,
            forecast,
            avgDaily,
            totalStock,
            daysUntilStockout);
    }

    public async Task<List<ForecastResult>> GetLowStockForecastsAsync(int daysThreshold = 14, CancellationToken ct = default)
    {
        // Get products with variants that have stock
        var productIds = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive && v.Stock > 0)
            .Select(v => v.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var results = new List<ForecastResult>();
        foreach (var pid in productIds)
        {
            var forecast = await ForecastAsync(pid, 30, ct);
            if (forecast.EstimatedDaysUntilStockout <= daysThreshold && forecast.EstimatedDaysUntilStockout < int.MaxValue)
            {
                results.Add(forecast);
            }
        }

        return results.OrderBy(r => r.EstimatedDaysUntilStockout).ToList();
    }

    public async Task<ForecastResult> ForecastWithAiAsync(int productId, int forecastDays = 30, CancellationToken ct = default)
    {
        var forecast = await ForecastAsync(productId, forecastDays, ct);
        if (forecast.ProductName == "Unknown") return forecast;

        var category = await _db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.Category.Name)
            .FirstOrDefaultAsync(ct) ?? "기타";

        var insight = await _aiInsight.GenerateInsightAsync(
            forecast.ProductName, category, forecast.HistoricalDemand,
            forecast.ForecastedDemand, forecast.CurrentStock, ct);

        return forecast with { AiInsight = insight };
    }

    public async Task<CategoryForecastResult> ForecastCategoryAsync(int categoryId, int forecastDays = 30, CancellationToken ct = default)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null)
            return new CategoryForecastResult(categoryId, "Unknown", 0, 0, 0, int.MaxValue, []);

        var productIds = await _db.Products.AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var forecasts = new List<ForecastResult>();
        foreach (var pid in productIds)
        {
            forecasts.Add(await ForecastAsync(pid, forecastDays, ct));
        }

        var totalStock = forecasts.Sum(f => f.CurrentStock);
        var totalAvgDemand = forecasts.Sum(f => f.AverageDailyDemand);
        var minDays = forecasts.Count > 0
            ? forecasts.Min(f => f.EstimatedDaysUntilStockout)
            : int.MaxValue;
        var topProducts = forecasts.OrderBy(f => f.EstimatedDaysUntilStockout).Take(5).ToList();

        var insight = await _aiInsight.GenerateCategoryInsightAsync(
            category.Name, productIds.Count, totalAvgDemand, totalStock, minDays, ct);

        return new CategoryForecastResult(
            categoryId, category.Name, productIds.Count, totalStock,
            Math.Round(totalAvgDemand, 1), minDays, topProducts, insight);
    }

    public async Task<List<CategoryForecastResult>> GetAllCategoryForecastsAsync(int forecastDays = 30, CancellationToken ct = default)
    {
        var categoryIds = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var results = new List<CategoryForecastResult>();
        foreach (var cid in categoryIds)
        {
            results.Add(await ForecastCategoryAsync(cid, forecastDays, ct));
        }

        return results.OrderBy(r => r.MinDaysUntilStockout).ToList();
    }

    public async Task<List<PurchaseRecommendation>> GetPurchaseRecommendationsAsync(int daysThreshold = 14, CancellationToken ct = default)
    {
        var lowStockForecasts = await GetLowStockForecastsAsync(daysThreshold, ct);
        var recommendations = new List<PurchaseRecommendation>();

        foreach (var f in lowStockForecasts)
        {
            var categoryName = await _db.Products.AsNoTracking()
                .Where(p => p.Id == f.ProductId)
                .Select(p => p.Category.Name)
                .FirstOrDefaultAsync(ct);

            const double leadTimeBuffer = 1.5; // 1.5x safety buffer
            var recommendedQty = (int)Math.Ceiling(f.AverageDailyDemand * 30 * leadTimeBuffer);

            var urgency = f.EstimatedDaysUntilStockout switch
            {
                <= 3 => "Critical",
                <= 7 => "High",
                <= 14 => "Medium",
                _ => "Low"
            };

            var reason = f.EstimatedDaysUntilStockout <= 7
                ? $"재고 {f.CurrentStock}개, {f.EstimatedDaysUntilStockout}일 내 소진 예상"
                : $"일평균 수요 {f.AverageDailyDemand}개 대비 재고 부족 예상";

            recommendations.Add(new PurchaseRecommendation(
                f.ProductId, f.ProductName, categoryName, f.CurrentStock,
                f.AverageDailyDemand, f.EstimatedDaysUntilStockout,
                recommendedQty, urgency, reason));
        }

        return recommendations.OrderBy(r => r.EstimatedDaysUntilStockout).ToList();
    }
}
