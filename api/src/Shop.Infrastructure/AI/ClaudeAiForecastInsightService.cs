using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.AI;

public class ClaudeAiForecastInsightService : IAiForecastInsightService
{
    private readonly IAIChatProvider _chatProvider;
    private readonly ITenantContext _tenantContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ClaudeAiForecastInsightService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan BatchCacheDuration = TimeSpan.FromHours(2);

    public ClaudeAiForecastInsightService(
        IAIChatProvider chatProvider,
        ITenantContext tenantContext,
        IDistributedCache cache,
        ILogger<ClaudeAiForecastInsightService> logger)
    {
        _chatProvider = chatProvider;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public Task<AiInsight?> GenerateInsightAsync(
        string productName,
        string categoryName,
        List<DailyDemand> historicalDemand,
        List<DailyDemand> forecast,
        int currentStock,
        CancellationToken ct = default)
    {
        return GenerateInsightAsync(productName, categoryName, historicalDemand, forecast, currentStock,
            null, null, null, null, ct);
    }

    public async Task<AiInsight?> GenerateInsightAsync(
        string productName,
        string categoryName,
        List<DailyDemand> historicalDemand,
        List<DailyDemand> forecast,
        int currentStock,
        double? trendSlope,
        string? trendDirection,
        double? seasonalityStrength,
        double? mape,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"forecast:insight:{_tenantContext.TenantId}:{productName.GetHashCode()}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<AiInsight>(cached);

            var tenantDesc = GetTenantDescription();
            var demandSummary = BuildDemandSummary(historicalDemand);
            var forecastSummary = BuildDemandSummary(forecast);

            var contextLines = new List<string>();
            if (trendDirection is not null)
                contextLines.Add($"추세: {trendDirection} (기울기: {trendSlope:F3})");
            if (seasonalityStrength is not null)
                contextLines.Add($"계절성 강도: {seasonalityStrength:F3} (0=없음, 1=강함)");
            if (mape is not null)
                contextLines.Add($"예측 정확도 MAPE: {mape:F1}%");

            var contextSection = contextLines.Count > 0
                ? $"\n통계 분석 결과:\n{string.Join("\n", contextLines)}\n"
                : "";

            var conservativeNote = mape > 30
                ? "\n주의: 예측 정확도가 낮으므로 보수적으로 분석해주세요.\n"
                : "";

            var prompt = $$"""
                당신은 {{tenantDesc}} 수요 예측 분석 전문가입니다.
                다음 데이터를 분석하여 JSON 형식으로 인사이트를 제공해주세요.

                상품: {{productName}}
                카테고리: {{categoryName}}
                현재 재고: {{currentStock}}개
                {{contextSection}}{{conservativeNote}}
                최근 90일 판매 데이터 (주간 요약):
                {{demandSummary}}

                향후 예측:
                {{forecastSummary}}

                다음 이벤트/시즌도 고려하세요: 크리스마스, 부활절, 추석, 설날, 성탄절, 사순절

                반드시 아래 JSON 형식으로만 응답하세요:
                {
                  "trendAnalysis": "추세 분석 (2-3문장)",
                  "seasonalPatterns": "시즌 패턴 분석 (2-3문장)",
                  "recommendations": ["추천1", "추천2", "추천3"],
                  "eventImpact": "이벤트 영향 분석 (해당되는 경우, 없으면 null)",
                  "confidenceScore": 0.0~1.0
                }
                """;

            var response = await _chatProvider.ChatAsync(
                [new AiChatMessage("user", prompt)],
                "당신은 소매업 수요 예측 전문 분석가입니다. JSON으로만 응답하세요.",
                ct);

            if (response.Error is not null)
            {
                _logger.LogWarning("AI insight generation failed: {Error}", response.Error);
                return null;
            }

            var insight = ParseInsightResponse(response.Content);
            if (insight is not null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(insight),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);
            }

            return insight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI insight for {ProductName}", productName);
            return null;
        }
    }

    public async Task<AiInsight?> GenerateCategoryInsightAsync(
        string categoryName,
        int productCount,
        double totalAverageDailyDemand,
        int totalStock,
        int minDaysUntilStockout,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"forecast:insight:cat:{_tenantContext.TenantId}:{categoryName.GetHashCode()}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<AiInsight>(cached);

            var tenantDesc = GetTenantDescription();

            var stockoutText = minDaysUntilStockout >= 9999 ? "충분" : $"{minDaysUntilStockout}일";
            var prompt = $$"""
                당신은 {{tenantDesc}} 카테고리 수요 예측 전문가입니다.
                다음 카테고리 데이터를 분석하여 JSON 형식으로 인사이트를 제공해주세요.

                카테고리: {{categoryName}}
                상품 수: {{productCount}}개
                총 재고: {{totalStock}}개
                일평균 총 수요: {{totalAverageDailyDemand}}개
                최소 재고 소진 예상: {{stockoutText}}

                반드시 아래 JSON 형식으로만 응답하세요:
                {
                  "trendAnalysis": "카테고리 추세 분석 (2-3문장)",
                  "seasonalPatterns": "시즌 패턴 (2-3문장)",
                  "recommendations": ["추천1", "추천2", "추천3"],
                  "eventImpact": null,
                  "confidenceScore": 0.0~1.0
                }
                """;

            var response = await _chatProvider.ChatAsync(
                [new AiChatMessage("user", prompt)],
                "당신은 소매업 카테고리 수요 예측 전문 분석가입니다. JSON으로만 응답하세요.",
                ct);

            if (response.Error is not null) return null;

            var insight = ParseInsightResponse(response.Content);
            if (insight is not null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(insight),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);
            }

            return insight;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate category AI insight for {CategoryName}", categoryName);
            return null;
        }
    }

    public async Task<BatchAiInsightResult> GenerateBatchInsightAsync(
        List<PurchaseRecommendation> lowStockProducts,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"forecast:batch:{_tenantContext.TenantId}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                var cachedResult = JsonSerializer.Deserialize<BatchAiInsightResult>(cached);
                if (cachedResult is not null) return cachedResult;
            }

            var tenantDesc = GetTenantDescription();
            var productList = string.Join("\n", lowStockProducts.Select(p =>
                $"- {p.ProductName} (ID:{p.ProductId}, 카테고리:{p.CategoryName}, 재고:{p.CurrentStock}, 일수요:{p.AverageDailyDemand:F1}, 소진:{p.EstimatedDaysUntilStockout}일, 긴급도:{p.Urgency})"));

            var prompt = $$"""
                당신은 {{tenantDesc}} 재고 분석 전문가입니다.
                다음 저재고 상품들을 일괄 분석하여 JSON으로 응답하세요.

                저재고 상품 목록:
                {{productList}}

                반드시 아래 JSON 형식으로만 응답하세요:
                {
                  "products": [
                    {
                      "productId": 상품ID,
                      "productName": "상품명",
                      "trendDirection": "Rising|Falling|Stable",
                      "seasonalityStrength": 0.0~1.0,
                      "keyInsight": "핵심 인사이트 1문장"
                    }
                  ],
                  "overallSummary": "전체 재고 상황 요약 (2-3문장)",
                  "averageConfidence": 0.0~1.0
                }
                """;

            var response = await _chatProvider.ChatAsync(
                [new AiChatMessage("user", prompt)],
                "당신은 소매업 재고/수요 분석 전문가입니다. JSON으로만 응답하세요.",
                ct);

            if (response.Error is not null)
            {
                _logger.LogWarning("Batch AI insight failed: {Error}", response.Error);
                return new BatchAiInsightResult([], "AI 분석을 사용할 수 없습니다.", 0);
            }

            var result = ParseBatchInsightResponse(response.Content, lowStockProducts);
            if (result is not null)
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = BatchCacheDuration }, ct);
            }

            return result ?? new BatchAiInsightResult([], "AI 응답 파싱 실패", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate batch AI insights");
            return new BatchAiInsightResult([], "AI 분석 중 오류 발생", 0);
        }
    }

    private string GetTenantDescription()
    {
        var slug = _tenantContext.TenantSlug?.ToLowerInvariant() ?? "";
        return slug switch
        {
            "catholia" => "가톨릭 성물/종교용품 쇼핑몰",
            "mohyun" => "한국 전통 장류(된장/고추장/간장) 쇼핑몰",
            _ => "온라인 쇼핑몰"
        };
    }

    private static string BuildDemandSummary(List<DailyDemand> demand)
    {
        if (demand.Count == 0) return "데이터 없음";

        var weeks = demand
            .GroupBy(d => d.Date.AddDays(-(int)d.Date.DayOfWeek))
            .Select(g => $"{g.Key:MM/dd}~: 총 {g.Sum(x => x.Quantity)}개 (일평균 {g.Average(x => (double)x.Quantity):F1})")
            .ToList();

        return string.Join("\n", weeks);
    }

    private static AiInsight? ParseInsightResponse(string content)
    {
        try
        {
            var json = content;
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                json = content[jsonStart..(jsonEnd + 1)];

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recommendations = root.TryGetProperty("recommendations", out var recs)
                ? recs.EnumerateArray().Select(r => r.GetString() ?? "").ToArray()
                : [];

            var eventImpact = root.TryGetProperty("eventImpact", out var ei) && ei.ValueKind != JsonValueKind.Null
                ? ei.GetString()
                : null;

            var confidence = root.TryGetProperty("confidenceScore", out var cs)
                ? cs.GetDouble()
                : 0.5;

            return new AiInsight(
                TrendAnalysis: root.GetProperty("trendAnalysis").GetString() ?? "",
                SeasonalPatterns: root.GetProperty("seasonalPatterns").GetString() ?? "",
                Recommendations: recommendations,
                EventImpact: eventImpact,
                ConfidenceScore: Math.Clamp(confidence, 0, 1));
        }
        catch
        {
            return null;
        }
    }

    private static BatchAiInsightResult? ParseBatchInsightResponse(string content, List<PurchaseRecommendation> products)
    {
        try
        {
            var json = content;
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                json = content[jsonStart..(jsonEnd + 1)];

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var productSummaries = new List<ProductAiSummary>();
            if (root.TryGetProperty("products", out var prods))
            {
                foreach (var p in prods.EnumerateArray())
                {
                    productSummaries.Add(new ProductAiSummary(
                        ProductId: p.GetProperty("productId").GetInt32(),
                        ProductName: p.GetProperty("productName").GetString() ?? "",
                        TrendDirection: p.TryGetProperty("trendDirection", out var td) ? td.GetString() ?? "Stable" : "Stable",
                        SeasonalityStrength: p.TryGetProperty("seasonalityStrength", out var ss) ? ss.GetDouble() : 0,
                        KeyInsight: p.TryGetProperty("keyInsight", out var ki) ? ki.GetString() ?? "" : ""));
                }
            }

            var overallSummary = root.TryGetProperty("overallSummary", out var os) ? os.GetString() ?? "" : "";
            var avgConfidence = root.TryGetProperty("averageConfidence", out var ac) ? ac.GetDouble() : 0.5;

            return new BatchAiInsightResult(productSummaries, overallSummary, Math.Clamp(avgConfidence, 0, 1));
        }
        catch
        {
            return null;
        }
    }
}
