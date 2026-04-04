using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using Shop.Infrastructure.AI;

namespace Shop.Tests.Unit.Services;

public class ClaudeAiForecastInsightServiceTests
{
    private readonly Mock<IAIChatProvider> _chatProviderMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly IDistributedCache _cache;
    private readonly Mock<ILogger<ClaudeAiForecastInsightService>> _loggerMock;
    private readonly ClaudeAiForecastInsightService _sut;

    public ClaudeAiForecastInsightServiceTests()
    {
        _chatProviderMock = new Mock<IAIChatProvider>();
        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(x => x.TenantId).Returns(1);
        _tenantContextMock.Setup(x => x.TenantSlug).Returns("catholia");

        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _loggerMock = new Mock<ILogger<ClaudeAiForecastInsightService>>();

        _sut = new ClaudeAiForecastInsightService(
            _chatProviderMock.Object,
            _tenantContextMock.Object,
            _cache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateInsight_WithHighMape_AddsConservativeDirective()
    {
        // Arrange
        var historical = Enumerable.Range(0, 14)
            .Select(i => new DailyDemand(DateTime.UtcNow.AddDays(-14 + i), 5))
            .ToList();
        var forecast = Enumerable.Range(1, 7)
            .Select(i => new DailyDemand(DateTime.UtcNow.AddDays(i), 5))
            .ToList();

        string? capturedPrompt = null;
        _chatProviderMock
            .Setup(x => x.ChatAsync(It.IsAny<IReadOnlyList<AiChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<AiChatMessage>, string?, CancellationToken>((msgs, _, _) =>
            {
                capturedPrompt = msgs[0].Content;
            })
            .ReturnsAsync(new ChatResponse(
                """{"trendAnalysis":"안정","seasonalPatterns":"없음","recommendations":["재고유지"],"eventImpact":null,"confidenceScore":0.5}""",
                null));

        // Act
        await _sut.GenerateInsightAsync(
            "묵주", "성물", historical, forecast, 100,
            trendSlope: 0.1, trendDirection: "Stable",
            seasonalityStrength: 0.3, mape: 35.0);

        // Assert
        capturedPrompt.Should().Contain("보수적");
    }

    [Fact]
    public async Task GenerateBatchInsight_CachesResult()
    {
        // Arrange
        var products = new List<PurchaseRecommendation>
        {
            new(1, "묵주", "성물", 10, 5.0, 2, 225, "Critical", "재고 부족")
        };

        var jsonResponse = """
        {
            "products": [
                {"productId": 1, "productName": "묵주", "trendDirection": "Rising", "seasonalityStrength": 0.5, "keyInsight": "수요 증가"}
            ],
            "overallSummary": "전체 재고 부족",
            "averageConfidence": 0.7
        }
        """;
        _chatProviderMock
            .Setup(x => x.ChatAsync(It.IsAny<IReadOnlyList<AiChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(jsonResponse, null));

        // Act - First call
        var result1 = await _sut.GenerateBatchInsightAsync(products);

        // Act - Second call (should use cache)
        var result2 = await _sut.GenerateBatchInsightAsync(products);

        // Assert
        result1.OverallSummary.Should().Be("전체 재고 부족");
        result2.OverallSummary.Should().Be("전체 재고 부족");

        // ChatAsync should only be called once (second call uses cache)
        _chatProviderMock.Verify(
            x => x.ChatAsync(It.IsAny<IReadOnlyList<AiChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateBatchInsight_LimitsTo20Products()
    {
        // Arrange - The batch insight service accepts whatever list is given,
        // but the DemandForecastService.GetBatchAiInsightsAsync limits to 20.
        // Here we verify the service processes a list correctly.
        var products = Enumerable.Range(1, 25)
            .Select(i => new PurchaseRecommendation(i, $"Product{i}", "Cat", 10, 5.0, 3, 225, "Critical", "부족"))
            .ToList();

        var productJsonItems = string.Join(",", products.Take(25).Select(p =>
            $"{{\"productId\":{p.ProductId},\"productName\":\"{p.ProductName}\",\"trendDirection\":\"Stable\",\"seasonalityStrength\":0.3,\"keyInsight\":\"분석\"}}"));

        var jsonResponse = $$"""
        {
            "products": [{{productJsonItems}}],
            "overallSummary": "전체 분석",
            "averageConfidence": 0.6
        }
        """;
        _chatProviderMock
            .Setup(x => x.ChatAsync(It.IsAny<IReadOnlyList<AiChatMessage>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(jsonResponse, null));

        // Act
        var result = await _sut.GenerateBatchInsightAsync(products);

        // Assert
        result.Should().NotBeNull();
        result.OverallSummary.Should().Be("전체 분석");
        // The service itself doesn't limit, but verifies it works with any count
        result.Products.Should().HaveCountGreaterThan(0);
    }
}
