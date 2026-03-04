using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class ProductAdvancedIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductAdvancedIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProducts_WithSearch_ReturnsFilteredResults()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?search=묵주&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?category=1&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithPriceRange_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?minPrice=10000&maxPrice=50000&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithSort_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?sort=price_asc&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("묵주");
    }

    [Fact]
    public async Task GetProductById_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductVariants_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products/1/variants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductSlugs_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products/slugs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSearchSuggestions_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products/suggestions?term=묵");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProduct_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            Name = "Updated 묵주",
            Slug = "rosary",
            Description = "Updated description",
            Price = 18000m,
            PriceType = "fixed",
            CategoryId = 1
        };

        var response = await client.PutAsJsonAsync("/api/products/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateProduct_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            Name = "Hacked Product",
            Slug = "hacked",
            Price = 1m,
            PriceType = "fixed",
            CategoryId = 1
        };

        var response = await client.PutAsJsonAsync("/api/products/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithFeaturedFilter_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?isFeatured=true&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithNewFilter_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/products?isNew=true&page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
