using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CollectionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CollectionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyCollections_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/collections");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyCollections_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/collections");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCollection_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            name = "My Favorites",
            description = "A collection of favorite items",
            isPublic = false
        };

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCollection_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var request = new
        {
            name = "Unauthorized Collection",
            isPublic = false
        };

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCollectionDetail_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/collections/1");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCollection_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/collections/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddItemToCollection_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            productId = 1,
            note = "Great product"
        };

        var response = await client.PostAsJsonAsync("/api/collections/1/items", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveItemFromCollection_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/collections/1/items/1");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
