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

    public async Task<AiInsight?> GenerateInsightAsync(
        string productName,
        string categoryName,
        List<DailyDemand> historicalDemand,
        List<DailyDemand> forecast,
        int currentStock,
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

            var prompt = $$"""
                당신은 {{tenantDesc}} 수요 예측 분석 전문가입니다.
                다음 데이터를 분석하여 JSON 형식으로 인사이트를 제공해주세요.

                상품: {{productName}}
                카테고리: {{categoryName}}
                현재 재고: {{currentStock}}개

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
                [new ChatMessage("user", prompt)],
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
                [new ChatMessage("user", prompt)],
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

        // Group by week for concise summary
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
            // Extract JSON from potential markdown code blocks
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
}
