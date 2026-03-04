using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class WishlistIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WishlistIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWishlist_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWishlist_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/wishlist");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleWishlist_AddProduct_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { productId = 1 };

        var response = await client.PostAsJsonAsync("/api/wishlist/toggle", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ToggleWishlist_RemoveProduct_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { productId = 1 };

        // Add first
        await client.PostAsJsonAsync("/api/wishlist/toggle", request);
        // Toggle again to remove
        var response = await client.PostAsJsonAsync("/api/wishlist/toggle", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CheckWishlist_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { productIds = new[] { 1, 2 } };

        var response = await client.PostAsJsonAsync("/api/wishlist/check", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
