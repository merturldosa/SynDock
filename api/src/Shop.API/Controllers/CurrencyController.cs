using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrencyController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    [HttpGet("rates")]
    public async Task<IActionResult> GetRates([FromQuery] string baseCurrency = "KRW", CancellationToken ct = default)
    {
        var rates = await _currencyService.GetRatesAsync(baseCurrency);
        return Ok(new
        {
            baseCurrency,
            rates,
            supportedCurrencies = _currencyService.SupportedCurrencies,
            updatedAt = DateTime.UtcNow
        });
    }

    [HttpGet("convert")]
    public async Task<IActionResult> Convert(
        [FromQuery] decimal amount,
        [FromQuery] string from = "KRW",
        [FromQuery] string to = "USD",
        CancellationToken ct = default)
    {
        var converted = await _currencyService.ConvertAsync(amount, from, to);
        return Ok(new
        {
            from,
            to,
            originalAmount = amount,
            convertedAmount = converted
        });
    }
}
