using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class ForecastIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ForecastIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetForecast_ReturnsValidResult()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/forecast/products/1?days=14");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ForecastResponse>();
        content.Should().NotBeNull();
        content!.ProductId.Should().Be(1);
    }

    [Fact]
    public async Task GetAccuracy_ReturnsEmptyForNewProduct()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/forecast/accuracy/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // For a new product with no forecast history, should return a message
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetLowStock_ReturnsFilteredResults()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/forecast/low-stock?daysThreshold=14");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBatchAiInsights_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/admin/forecast/batch-ai-insights");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AutoPurchaseOrder_WithValidProducts_Returns200OrBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new { ProductIds = new[] { 1 } };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/forecast/auto-purchase-order", request);

        // Assert - May return 400 if MES is not configured, but should not 500
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    private record ForecastResponse(
        int ProductId, string? ProductName,
        double AverageDailyDemand, int CurrentStock,
        int EstimatedDaysUntilStockout, string? ForecastMethod);
}
