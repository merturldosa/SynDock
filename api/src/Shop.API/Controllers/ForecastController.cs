using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
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
    public async Task<IActionResult> GetProductForecast(int productId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _forecastService.ForecastAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockForecasts([FromQuery] int daysThreshold = 14, CancellationToken ct = default)
    {
        var results = await _forecastService.GetLowStockForecastsAsync(daysThreshold);
        return Ok(results);
    }

    [HttpGet("products/{productId:int}/ai")]
    public async Task<IActionResult> GetProductForecastWithAi(int productId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _forecastService.ForecastWithAiAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategoryForecasts([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var results = await _forecastService.GetAllCategoryForecastsAsync(days);
        return Ok(results);
    }

    [HttpGet("categories/{categoryId:int}")]
    public async Task<IActionResult> GetCategoryForecast(int categoryId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _forecastService.ForecastCategoryAsync(categoryId, days);
        return Ok(result);
    }

    [HttpGet("purchase-recommendations")]
    public async Task<IActionResult> GetPurchaseRecommendations([FromQuery] int daysThreshold = 14, CancellationToken ct = default)
    {
        var results = await _forecastService.GetPurchaseRecommendationsAsync(daysThreshold);
        return Ok(results);
    }

    // Sprint 5: Holt-Winters & accuracy tracking

    [HttpGet("products/{productId:int}/holt-winters")]
    public async Task<IActionResult> GetHoltWintersForecast(int productId, [FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await _forecastService.ForecastHoltWintersAsync(productId, days);
        return Ok(result);
    }

    [HttpGet("accuracy/{productId:int}")]
    public async Task<IActionResult> GetProductAccuracy(int productId, CancellationToken ct)
    {
        var result = await _forecastService.GetAccuracyAsync(productId);
        if (result is null)
            return Ok(new { message = "No forecast accuracy data available." });
        return Ok(result);
    }

    [HttpGet("accuracy")]
    public async Task<IActionResult> GetAllAccuracies(CancellationToken ct)
    {
        var results = await _forecastService.GetAllAccuraciesAsync();
        return Ok(results);
    }

    [HttpPost("accuracy/update")]
    public async Task<IActionResult> UpdateAccuracy(CancellationToken ct)
    {
        await _forecastService.UpdateActualDemandAsync();
        return Ok(new { message = "Accuracy data has been updated." });
    }

    [HttpPost("auto-purchase-order")]
    public async Task<IActionResult> CreateAutoPurchaseOrder([FromBody] AutoPurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await _forecastService.CreateAutoPurchaseOrderAsync(request.ProductIds);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("batch-ai-insights")]
    public async Task<IActionResult> GetBatchAiInsights(CancellationToken ct)
    {
        var result = await _forecastService.GetBatchAiInsightsAsync();
        return Ok(result);
    }
}

public record AutoPurchaseOrderRequest(List<int> ProductIds);
