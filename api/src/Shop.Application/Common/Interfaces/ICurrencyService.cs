namespace Shop.Application.Common.Interfaces;

public interface ICurrencyService
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, CancellationToken ct = default);
    Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency = "KRW", CancellationToken ct = default);
    IReadOnlyList<string> SupportedCurrencies { get; }
}
