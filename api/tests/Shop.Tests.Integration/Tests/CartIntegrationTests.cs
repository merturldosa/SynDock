using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CartIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCart_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCart_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToCart_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            productId = 1,
            quantity = 1
        };

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddToCart_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var request = new
        {
            productId = 1,
            quantity = 1
        };

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCartItem_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new { quantity = 3 };

        var response = await client.PutAsJsonAsync("/api/cart/items/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveCartItem_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/cart/items/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClearCart_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/cart");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
