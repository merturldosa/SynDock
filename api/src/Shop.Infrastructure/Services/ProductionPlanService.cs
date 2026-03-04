using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Services;

public class ProductionPlanService : IProductionPlanService
{
    private readonly IShopDbContext _db;
    private readonly IDemandForecastService _forecast;
    private readonly IMesClient _mesClient;
    private readonly IMesProductMapper _productMapper;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProductionPlanService> _logger;

    public ProductionPlanService(
        IShopDbContext db,
        IDemandForecastService forecast,
        IMesClient mesClient,
        IMesProductMapper productMapper,
        ITenantContext tenantContext,
        ILogger<ProductionPlanService> logger)
    {
        _db = db;
        _forecast = forecast;
        _mesClient = mesClient;
        _productMapper = productMapper;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<List<ProductionSuggestionDto>> GenerateSuggestionsAsync(CancellationToken ct = default)
    {
        var recommendations = await _forecast.GetPurchaseRecommendationsAsync(21, ct);

        var suggestions = new List<ProductionPlanSuggestion>();

        foreach (var rec in recommendations)
        {
            // Check if pending suggestion already exists for this product
            var existing = await _db.ProductionPlanSuggestions
                .AnyAsync(s => s.ProductId == rec.ProductId && s.Status == "Pending", ct);
            if (existing) continue;

            ForecastResult? forecastResult = null;
            try { forecastResult = await _forecast.ForecastWithAiAsync(rec.ProductId, 30, ct); }
            catch { /* AI forecast may fail, continue without it */ }

            var suggestion = new ProductionPlanSuggestion
            {
                TenantId = _tenantContext.TenantId,
                ProductId = rec.ProductId,
                ProductName = rec.ProductName,
                CurrentStock = rec.CurrentStock,
                AverageDailySales = rec.AverageDailyDemand,
                EstimatedDaysUntilStockout = rec.EstimatedDaysUntilStockout,
                SuggestedQuantity = rec.RecommendedOrderQuantity,
                Urgency = rec.Urgency,
                Status = "Pending",
                AiReason = forecastResult?.AiInsight?.TrendAnalysis,
                TrendAnalysis = forecastResult?.TrendDirection,
                SeasonalityFactor = forecastResult?.SeasonalityStrength,
                ConfidenceScore = forecastResult?.AiInsight?.ConfidenceScore,
                CreatedBy = "ProductionPlanService"
            };

            suggestions.Add(suggestion);
        }

        if (suggestions.Count > 0)
        {
            await _db.ProductionPlanSuggestions.AddRangeAsync(suggestions, ct);
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("생산계획 제안 {Count}건 생성", suggestions.Count);

        return suggestions.Select(MapToDto).ToList();
    }

    public async Task<ProductionSuggestionDto?> ApproveSuggestionAsync(int suggestionId, string approvedBy, CancellationToken ct = default)
    {
        var suggestion = await _db.ProductionPlanSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId, ct);

        if (suggestion is null || suggestion.Status != "Pending") return null;

        suggestion.Status = "Approved";
        suggestion.ApprovedAt = DateTime.UtcNow;
        suggestion.ApprovedBy = approvedBy;
        suggestion.UpdatedBy = approvedBy;
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapToDto(suggestion);
    }

    public async Task<bool> RejectSuggestionAsync(int suggestionId, string reason, CancellationToken ct = default)
    {
        var suggestion = await _db.ProductionPlanSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId, ct);

        if (suggestion is null || suggestion.Status != "Pending") return false;

        suggestion.Status = "Rejected";
        suggestion.RejectionReason = reason;
        suggestion.UpdatedBy = "Admin";
        suggestion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> ForwardToMesAsync(int suggestionId, CancellationToken ct = default)
    {
        var suggestion = await _db.ProductionPlanSuggestions
            .FirstOrDefaultAsync(s => s.Id == suggestionId && s.Status == "Approved", ct);

        if (suggestion is null) return null;

        var mesProductCode = await _productMapper.GetMesProductCodeAsync(suggestion.ProductId, ct);
        if (mesProductCode is null)
        {
            _logger.LogWarning("MES 상품 매핑 없음: ProductId={ProductId}", suggestion.ProductId);
            return null;
        }

        try
        {
            var orderRequest = new MesSalesOrderRequest(
                OrderNo: $"PP-{suggestion.Id}-{DateTime.UtcNow:yyyyMMddHHmm}",
                OrderDate: DateTime.UtcNow.ToString("yyyy-MM-dd"),
                CustomerId: 1,
                SalesUserId: 1,
                Items: [
                    new MesSalesOrderLine(1, long.Parse(mesProductCode), suggestion.SuggestedQuantity, "EA", 0)
                ]
            );

            var result = await _mesClient.CreateSalesOrderAsync(orderRequest, ct);

            if (result.Success)
            {
                suggestion.Status = "Forwarded";
                suggestion.MesOrderId = result.MesOrderId;
                suggestion.UpdatedBy = "MES";
                suggestion.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("생산계획 MES 전송 성공: SuggestionId={Id}, MesOrderId={MesOrderId}",
                    suggestionId, result.MesOrderId);
                return result.MesOrderId;
            }

            _logger.LogWarning("MES 주문 생성 실패: {Error}", result.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MES 전송 예외: SuggestionId={Id}", suggestionId);
            return null;
        }
    }

    private static ProductionSuggestionDto MapToDto(ProductionPlanSuggestion s) => new(
        s.Id, s.ProductId, s.ProductName, s.CurrentStock,
        s.AverageDailySales, s.EstimatedDaysUntilStockout,
        s.SuggestedQuantity, s.Urgency, s.Status,
        s.AiReason, s.TrendAnalysis, s.SeasonalityFactor, s.ConfidenceScore,
        s.MesOrderId, s.ApprovedAt, s.ApprovedBy, s.CreatedAt);
}
