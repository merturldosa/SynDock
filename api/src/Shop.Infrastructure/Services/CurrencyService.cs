using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ILogger<CurrencyService> _logger;
    private readonly IDistributedCache? _cache;
    private const string CacheKey = "currency:rates:KRW";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);

    // Fallback rates (KRW base, approximate)
    private static readonly Dictionary<string, decimal> FallbackRates = new()
    {
        ["KRW"] = 1m,
        ["USD"] = 0.00074m,
        ["EUR"] = 0.00068m,
        ["JPY"] = 0.11m,
        ["CNY"] = 0.0053m,
        ["VND"] = 18.5m,
    };

    public IReadOnlyList<string> SupportedCurrencies { get; } = new List<string>
    {
        "KRW", "USD", "EUR", "JPY", "CNY", "VND"
    };

    public CurrencyService(ILogger<CurrencyService> logger, IDistributedCache? cache = null)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken ct = default)
    {
        if (fromCurrency == toCurrency) return amount;

        var rates = await GetRatesAsync("KRW", ct);

        if (!rates.TryGetValue(fromCurrency, out var fromRate) ||
            !rates.TryGetValue(toCurrency, out var toRate))
        {
            _logger.LogWarning("Currency not found: {From} or {To}", fromCurrency, toCurrency);
            return amount;
        }

        // Convert: amount in fromCurrency → KRW → toCurrency
        var krwAmount = fromRate == 0 ? 0 : amount / fromRate;
        return Math.Round(krwAmount * toRate, 2);
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency = "KRW", CancellationToken ct = default)
    {
        if (_cache != null)
        {
            try
            {
                var cached = await _cache.GetStringAsync(CacheKey, ct);
                if (!string.IsNullOrEmpty(cached))
                {
                    var cachedRates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(cached);
                    if (cachedRates != null) return cachedRates;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read currency cache");
            }
        }

        // Use fallback rates (in production, fetch from external API)
        var rates = new Dictionary<string, decimal>(FallbackRates);

        if (_cache != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(rates);
                await _cache.SetStringAsync(CacheKey, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheExpiry
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache currency rates");
            }
        }

        return rates;
    }
}
