using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/admin/forecast")]
[Authorize]
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
}
