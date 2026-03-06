using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shop.Infrastructure.Services;

namespace Shop.Tests.Unit.Services;

public class CurrencyServiceTests
{
    private readonly CurrencyService _sut;

    public CurrencyServiceTests()
    {
        var logger = new Mock<ILogger<CurrencyService>>();
        _sut = new CurrencyService(logger.Object);
    }

    [Fact]
    public void SupportedCurrencies_ContainsSixCurrencies()
    {
        _sut.SupportedCurrencies.Should().HaveCount(6);
        _sut.SupportedCurrencies.Should().Contain(new[] { "KRW", "USD", "EUR", "JPY", "CNY", "VND" });
    }

    [Fact]
    public async Task ConvertAsync_SameCurrency_ReturnsSameAmount()
    {
        var result = await _sut.ConvertAsync(10000m, "KRW", "KRW");
        result.Should().Be(10000m);
    }

    [Fact]
    public async Task ConvertAsync_KRWtoUSD_ReturnsPositiveValue()
    {
        var result = await _sut.ConvertAsync(1000000m, "KRW", "USD");
        result.Should().BeGreaterThan(0);
        result.Should().BeLessThan(1000000m);
    }

    [Fact]
    public async Task ConvertAsync_USDtoKRW_ReturnsLargerValue()
    {
        var result = await _sut.ConvertAsync(100m, "USD", "KRW");
        result.Should().BeGreaterThan(100m);
    }

    [Fact]
    public async Task ConvertAsync_UnknownCurrency_ReturnsSameAmount()
    {
        var result = await _sut.ConvertAsync(1000m, "KRW", "XYZ");
        result.Should().Be(1000m);
    }

    [Fact]
    public async Task GetRatesAsync_ReturnsFallbackRates()
    {
        var rates = await _sut.GetRatesAsync();
        rates.Should().ContainKey("KRW");
        rates.Should().ContainKey("USD");
        rates["KRW"].Should().Be(1m);
        rates["USD"].Should().BeGreaterThan(0);
    }
}
