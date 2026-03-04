using System.Net;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class RecommendationsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecommendationsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProductRecommendations_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/recommendations/product/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPopularProducts_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/recommendations/popular");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserRecommendations_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/recommendations/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserRecommendations_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/recommendations/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
