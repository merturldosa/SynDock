using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.AI;

/// <summary>
/// Demand forecast service with Holt-Winters triple exponential smoothing.
/// Falls back to simple moving average when insufficient data.
/// </summary>
public class DemandForecastService : IDemandForecastService
{
    private readonly IShopDbContext _db;
    private readonly IAiForecastInsightService _aiInsight;
    private readonly ITenantContext _tenantContext;
    private readonly IMesClient _mesClient;
    private readonly IMesProductMapper _mesMapper;
    private readonly double _alpha;
    private readonly double _beta;
    private readonly double _gamma;
    private readonly int _seasonLength;

    public DemandForecastService(
        IShopDbContext db,
        IAiForecastInsightService aiInsight,
        ITenantContext tenantContext,
        IMesClient mesClient,
        IMesProductMapper mesMapper,
        IConfiguration configuration)
    {
        _db = db;
        _aiInsight = aiInsight;
        _tenantContext = tenantContext;
        _mesClient = mesClient;
        _mesMapper = mesMapper;
        _alpha = configuration.GetValue("Forecast:HoltWinters:Alpha", 0.3);
        _beta = configuration.GetValue("Forecast:HoltWinters:Beta", 0.1);
        _gamma = configuration.GetValue("Forecast:HoltWinters:Gamma", 0.2);
        _seasonLength = configuration.GetValue("Forecast:HoltWinters:SeasonLength", 7);
    }

    public async Task<ForecastResult> ForecastAsync(int productId, int forecastDays = 30, CancellationToken ct = default)
    {
        var (product, totalStock, historicalDemand) = await GetProductData(productId, ct);
        if (product is null)
            return new ForecastResult(productId, "Unknown", [], [], 0, 0, int.MaxValue);

        // Use Holt-Winters if sufficient data, else fallback to simple MA
        var hasEnoughData = historicalDemand.Count >= _seasonLength * 3;
        List<DailyDemand> forecast;
        string method;
        double trendSlope;
        double seasonalityStrength;

        if (hasEnoughData)
        {
            var quantities = historicalDemand.Select(h => (double)h.Quantity).ToArray();
            forecast = HoltWintersTripleSmooth(quantities, historicalDemand, forecastDays);
            method = "HoltWinters";
            trendSlope = CalculateTrendSlope(historicalDemand);
            seasonalityStrength = CalculateSeasonalityStrength(historicalDemand);
        }
        else
        {
            forecast = SimpleMovingAverageForecast(historicalDemand, forecastDays);
            method = "SimpleMA";
            trendSlope = CalculateTrendSlope(historicalDemand);
            seasonalityStrength = 0;
        }

        // Apply monthly seasonality overlay
        forecast = ApplyMonthlySeasonality(forecast, historicalDemand);

        var avgDaily = forecast.Count > 0 ? Math.Round(forecast.Average(f => (double)f.Quantity), 1) : 0;
        var daysUntilStockout = avgDaily > 0 ? (int)Math.Floor(totalStock / avgDaily) : int.MaxValue;

        var trendDirection = trendSlope switch
        {
            > 0.5 => "Rising",
            < -0.5 => "Falling",
            _ => "Stable"
        };

        return new ForecastResult(
            productId, product.Name, historicalDemand, forecast,
            avgDaily, totalStock, daysUntilStockout,
            TrendSlope: Math.Round(trendSlope, 3),
            TrendDirection: trendDirection,
            SeasonalityStrength: Math.Round(seasonalityStrength, 3),
            ForecastMethod: method);
    }

    public async Task<ForecastResult> ForecastHoltWintersAsync(int productId, int forecastDays = 30, CancellationToken ct = default)
    {
        return await ForecastAsync(productId, forecastDays, ct);
    }

    public async Task<List<ForecastResult>> GetLowStockForecastsAsync(int daysThreshold = 14, CancellationToken ct = default)
    {
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

        // Use enriched AI insight with trend/seasonality context
        var accuracy = await GetAccuracyAsync(productId, ct);
        var mape = accuracy?.Mape;

        var insight = await _aiInsight.GenerateInsightAsync(
            forecast.ProductName, category, forecast.HistoricalDemand,
            forecast.ForecastedDemand, forecast.CurrentStock,
            forecast.TrendSlope, forecast.TrendDirection,
            forecast.SeasonalityStrength, mape, ct);

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

            const double leadTimeBuffer = 1.5;
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

    // --- Sprint 5: Accuracy tracking ---

    public async Task RecordForecastAsync(int productId, CancellationToken ct = default)
    {
        var forecast = await ForecastAsync(productId, 14, ct);
        if (forecast.ProductName == "Unknown") return;

        var now = DateTime.UtcNow;
        var records = forecast.ForecastedDemand.Select(fd => new ForecastAccuracy
        {
            ProductId = productId,
            ForecastDate = now,
            TargetDate = fd.Date,
            PredictedQuantity = fd.Quantity
        }).ToList();

        _db.ForecastAccuracies.AddRange(records);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateActualDemandAsync(CancellationToken ct = default)
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var pending = await _db.ForecastAccuracies
            .Where(fa => fa.TargetDate.Date <= yesterday && fa.ActualQuantity == null)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        // Group by target date to batch query order items
        var dateGroups = pending.GroupBy(p => p.TargetDate.Date).ToList();
        foreach (var group in dateGroups)
        {
            var date = group.Key;
            var nextDate = date.AddDays(1);

            var dailySales = await _db.OrderItems.AsNoTracking()
                .Where(oi => oi.Order.CreatedAt >= date && oi.Order.CreatedAt < nextDate)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync(ct);

            var salesDict = dailySales.ToDictionary(s => s.ProductId, s => s.Quantity);

            foreach (var record in group)
            {
                var actual = salesDict.GetValueOrDefault(record.ProductId, 0);
                record.ActualQuantity = actual;
                record.AbsoluteError = Math.Abs(record.PredictedQuantity - actual);
                record.PercentageError = actual > 0
                    ? Math.Abs(record.PredictedQuantity - actual) / actual * 100
                    : record.PredictedQuantity > 0 ? 100 : 0;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ForecastAccuracyResult?> GetAccuracyAsync(int productId, CancellationToken ct = default)
    {
        var records = await _db.ForecastAccuracies.AsNoTracking()
            .Where(fa => fa.ProductId == productId && fa.ActualQuantity != null)
            .OrderByDescending(fa => fa.TargetDate)
            .Take(30)
            .ToListAsync(ct);

        if (records.Count == 0) return null;

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        var mape = records.Average(r => r.PercentageError ?? 0);
        var mae = records.Average(r => r.AbsoluteError ?? 0);

        var dataPoints = records.Select(r => new AccuracyDataPoint(
            r.TargetDate, r.PredictedQuantity, r.ActualQuantity ?? 0, r.PercentageError ?? 0
        )).ToList();

        return new ForecastAccuracyResult(
            productId, product?.Name ?? "Unknown", Math.Round(mape, 1), Math.Round(mae, 1),
            records.Count, dataPoints);
    }

    public async Task<List<ForecastAccuracyResult>> GetAllAccuraciesAsync(CancellationToken ct = default)
    {
        var productIds = await _db.ForecastAccuracies.AsNoTracking()
            .Where(fa => fa.ActualQuantity != null)
            .Select(fa => fa.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var results = new List<ForecastAccuracyResult>();
        foreach (var pid in productIds)
        {
            var accuracy = await GetAccuracyAsync(pid, ct);
            if (accuracy is not null)
                results.Add(accuracy);
        }

        return results.OrderBy(r => r.Mape).ToList();
    }

    public async Task<AutoPurchaseOrderResult> CreateAutoPurchaseOrderAsync(List<int> productIds, CancellationToken ct = default)
    {
        try
        {
            var lines = new List<MesSalesOrderLine>();
            var lineNo = 0;
            var totalQty = 0;

            foreach (var pid in productIds)
            {
                var mesCode = await _mesMapper.GetMesProductCodeAsync(pid, ct);
                if (mesCode is null) continue;

                var forecast = await ForecastAsync(pid, 30, ct);
                var recommendedQty = (int)Math.Ceiling(forecast.AverageDailyDemand * 30 * 1.5);
                if (recommendedQty <= 0) continue;

                lineNo++;
                totalQty += recommendedQty;
                lines.Add(new MesSalesOrderLine(lineNo, pid, recommendedQty, "EA", 0));
            }

            if (lines.Count == 0)
                return new AutoPurchaseOrderResult(false, null, 0, 0, "MES 매핑된 상품이 없습니다.");

            var request = new MesSalesOrderRequest(
                OrderNo: $"AUTO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                OrderDate: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                CustomerId: 1,
                SalesUserId: 1,
                Items: lines);

            var result = await _mesClient.CreateSalesOrderAsync(request, ct);

            return new AutoPurchaseOrderResult(
                result.Success, result.MesOrderId, lines.Count, totalQty, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new AutoPurchaseOrderResult(false, null, 0, 0, ex.Message);
        }
    }

    public async Task<BatchAiInsightResult> GetBatchAiInsightsAsync(CancellationToken ct = default)
    {
        var recommendations = await GetPurchaseRecommendationsAsync(14, ct);
        var top = recommendations.Take(20).ToList();

        if (top.Count == 0)
            return new BatchAiInsightResult([], "발주 추천 상품이 없습니다.", 0);

        return await _aiInsight.GenerateBatchInsightAsync(top, ct);
    }

    // --- Private methods ---

    private async Task<(Product? product, int totalStock, List<DailyDemand> history)> GetProductData(int productId, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
            return (null, 0, []);

        var totalStock = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId && v.IsActive)
            .SumAsync(v => v.Stock, ct);

        var since = DateTime.UtcNow.AddDays(-90);
        var orderData = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.ProductId == productId && oi.Order.CreatedAt >= since)
            .Select(oi => new { oi.Order.CreatedAt, oi.Quantity })
            .ToListAsync(ct);

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

        return (product, totalStock, historicalDemand);
    }

    private List<DailyDemand> HoltWintersTripleSmooth(double[] data, List<DailyDemand> historical, int forecastDays)
    {
        var n = data.Length;
        if (n < _seasonLength * 2)
            return SimpleMovingAverageForecast(historical, forecastDays);

        // Initialize level, trend, seasonal
        var level = data.Take(_seasonLength).Average();
        var trend = 0.0;
        if (n >= _seasonLength * 2)
        {
            var first = data.Take(_seasonLength).Average();
            var second = data.Skip(_seasonLength).Take(_seasonLength).Average();
            trend = (second - first) / _seasonLength;
        }

        var seasonal = new double[_seasonLength];
        for (var i = 0; i < _seasonLength; i++)
        {
            var avg = data.Take(_seasonLength).Average();
            seasonal[i] = avg > 0 ? data[i] / avg : 1.0;
        }

        // Run Holt-Winters on historical data
        for (var t = _seasonLength; t < n; t++)
        {
            var sIdx = t % _seasonLength;
            var val = data[t];
            var prevSeasonal = seasonal[sIdx];

            var newLevel = _alpha * (val / (prevSeasonal > 0 ? prevSeasonal : 1)) + (1 - _alpha) * (level + trend);
            var newTrend = _beta * (newLevel - level) + (1 - _beta) * trend;
            seasonal[sIdx] = _gamma * (val / (newLevel > 0 ? newLevel : 1)) + (1 - _gamma) * prevSeasonal;

            level = newLevel;
            trend = newTrend;
        }

        // Generate forecast
        var forecast = new List<DailyDemand>();
        for (var i = 1; i <= forecastDays; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(i);
            var sIdx = (n + i) % _seasonLength;
            var predicted = (int)Math.Round((level + trend * i) * seasonal[sIdx]);
            forecast.Add(new DailyDemand(date, Math.Max(0, predicted)));
        }

        return forecast;
    }

    private static List<DailyDemand> SimpleMovingAverageForecast(List<DailyDemand> historical, int forecastDays)
    {
        // Day-of-week seasonality
        var dowFactors = new double[7];
        var dowCounts = new int[7];
        foreach (var h in historical)
        {
            var dow = (int)h.Date.DayOfWeek;
            dowFactors[dow] += h.Quantity;
            dowCounts[dow]++;
        }
        for (var i = 0; i < 7; i++)
            dowFactors[i] = dowCounts[i] > 0 ? dowFactors[i] / dowCounts[i] : 0;

        var avgFactor = dowFactors.Average();
        if (avgFactor > 0)
            for (var i = 0; i < 7; i++)
                dowFactors[i] /= avgFactor;
        else
            Array.Fill(dowFactors, 1.0);

        var recent7 = historical.TakeLast(7).Average(h => (double)h.Quantity);
        var recent30 = historical.TakeLast(30).Average(h => (double)h.Quantity);
        var baseDemand = recent7 * 0.6 + recent30 * 0.4;

        var forecast = new List<DailyDemand>();
        for (var i = 1; i <= forecastDays; i++)
        {
            var date = DateTime.UtcNow.Date.AddDays(i);
            var dow = (int)date.DayOfWeek;
            var predicted = (int)Math.Round(baseDemand * dowFactors[dow]);
            forecast.Add(new DailyDemand(date, Math.Max(0, predicted)));
        }

        return forecast;
    }

    private static List<DailyDemand> ApplyMonthlySeasonality(List<DailyDemand> forecast, List<DailyDemand> historical)
    {
        // Calculate monthly factors from historical data
        var monthlyTotals = new double[12];
        var monthlyCounts = new int[12];
        foreach (var h in historical)
        {
            monthlyTotals[h.Date.Month - 1] += h.Quantity;
            monthlyCounts[h.Date.Month - 1]++;
        }

        var monthlyAvg = new double[12];
        for (var i = 0; i < 12; i++)
            monthlyAvg[i] = monthlyCounts[i] > 0 ? monthlyTotals[i] / monthlyCounts[i] : 0;

        var overallAvg = monthlyAvg.Where(m => m > 0).DefaultIfEmpty(1).Average();
        if (overallAvg <= 0) return forecast;

        var monthlyFactors = new double[12];
        for (var i = 0; i < 12; i++)
            monthlyFactors[i] = monthlyAvg[i] > 0 ? monthlyAvg[i] / overallAvg : 1.0;

        // Apply monthly seasonality with dampening (blend 70% original + 30% seasonal)
        return forecast.Select(f =>
        {
            var factor = monthlyFactors[f.Date.Month - 1];
            var adjusted = (int)Math.Round(f.Quantity * (0.7 + 0.3 * factor));
            return new DailyDemand(f.Date, Math.Max(0, adjusted));
        }).ToList();
    }

    private static double CalculateTrendSlope(List<DailyDemand> historical)
    {
        var recent30 = historical.TakeLast(30).ToList();
        if (recent30.Count < 7) return 0;

        // Simple linear regression
        var n = recent30.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (var i = 0; i < n; i++)
        {
            sumX += i;
            sumY += recent30[i].Quantity;
            sumXY += i * recent30[i].Quantity;
            sumX2 += i * i;
        }

        var denom = n * sumX2 - sumX * sumX;
        return denom != 0 ? (n * sumXY - sumX * sumY) / denom : 0;
    }

    private double CalculateSeasonalityStrength(List<DailyDemand> historical)
    {
        if (historical.Count < _seasonLength * 2) return 0;

        // Measure how much day-of-week variance explains total variance
        var overall = historical.Select(h => (double)h.Quantity).ToArray();
        var overallMean = overall.Average();
        var totalVariance = overall.Sum(v => Math.Pow(v - overallMean, 2));
        if (totalVariance <= 0) return 0;

        var dowMeans = Enumerable.Range(0, 7)
            .Select(dow =>
            {
                var vals = historical.Where(h => (int)h.Date.DayOfWeek == dow).Select(h => (double)h.Quantity);
                return vals.Any() ? vals.Average() : overallMean;
            }).ToArray();

        var seasonalVariance = historical.Sum(h => Math.Pow(dowMeans[(int)h.Date.DayOfWeek] - overallMean, 2));

        return Math.Min(1.0, seasonalVariance / totalVariance);
    }
}
