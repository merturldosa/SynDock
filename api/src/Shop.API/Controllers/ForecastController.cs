using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/admin/forecast")]
[Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
public class ForecastController : ControllerBase
{
    private readonly IDemandForecastService _forecastService;

    public ForecastController(IDemandForecastService forecastService)
    {
        _forecastService = forecastService;
    }

    [HttpGet("products/{productId:int}")]
    public async Task<IActionResult> GetProductForecast(int productId, [FromQuery] int days = 30)
    {
        var result = await _forecastService.ForecastAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockForecasts([FromQuery] int daysThreshold = 14)
    {
        var results = await _forecastService.GetLowStockForecastsAsync(daysThreshold);
        return Ok(results);
    }

    [HttpGet("products/{productId:int}/ai")]
    public async Task<IActionResult> GetProductForecastWithAi(int productId, [FromQuery] int days = 30)
    {
        var result = await _forecastService.ForecastWithAiAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategoryForecasts([FromQuery] int days = 30)
    {
        var results = await _forecastService.GetAllCategoryForecastsAsync(days);
        return Ok(results);
    }

    [HttpGet("categories/{categoryId:int}")]
    public async Task<IActionResult> GetCategoryForecast(int categoryId, [FromQuery] int days = 30)
    {
        var result = await _forecastService.ForecastCategoryAsync(categoryId, days);
        return Ok(result);
    }

    [HttpGet("purchase-recommendations")]
    public async Task<IActionResult> GetPurchaseRecommendations([FromQuery] int daysThreshold = 14)
    {
        var results = await _forecastService.GetPurchaseRecommendationsAsync(daysThreshold);
        return Ok(results);
    }

    // Sprint 5: Holt-Winters & accuracy tracking

    [HttpGet("products/{productId:int}/holt-winters")]
    public async Task<IActionResult> GetHoltWintersForecast(int productId, [FromQuery] int days = 30)
    {
        var result = await _forecastService.ForecastHoltWintersAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("accuracy/{productId:int}")]
    public async Task<IActionResult> GetProductAccuracy(int productId)
    {
        var result = await _forecastService.GetAccuracyAsync(productId);
        if (result is null)
            return Ok(new { message = "예측 정확도 데이터가 없습니다." });
        return Ok(result);
    }

    [HttpGet("accuracy")]
    public async Task<IActionResult> GetAllAccuracies()
    {
        var results = await _forecastService.GetAllAccuraciesAsync();
        return Ok(results);
    }

    [HttpPost("accuracy/update")]
    public async Task<IActionResult> UpdateAccuracy()
    {
        await _forecastService.UpdateActualDemandAsync();
        return Ok(new { message = "정확도 데이터가 갱신되었습니다." });
    }

    [HttpPost("auto-purchase-order")]
    public async Task<IActionResult> CreateAutoPurchaseOrder([FromBody] AutoPurchaseOrderRequest request)
    {
        var result = await _forecastService.CreateAutoPurchaseOrderAsync(request.ProductIds);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("batch-ai-insights")]
    public async Task<IActionResult> GetBatchAiInsights()
    {
        var result = await _forecastService.GetBatchAiInsightsAsync();
        return Ok(result);
    }
}

public record AutoPurchaseOrderRequest(List<int> ProductIds);
