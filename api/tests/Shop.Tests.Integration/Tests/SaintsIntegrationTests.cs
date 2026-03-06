using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class SaintsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SaintsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSaintsList_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/saints");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSaint_NonExistentId_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/saints/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSaintsToday_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/saints/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SeedSaints_AsAdmin_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Act
        var response = await client.PostAsync("/api/saints/seed", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SeedSaints_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.PostAsync("/api/saints/seed", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductsBySaint_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/saints/1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
