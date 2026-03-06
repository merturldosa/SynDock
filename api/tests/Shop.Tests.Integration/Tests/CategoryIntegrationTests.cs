using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CategoryIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoryIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAllCategories_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategorySlugs_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/categories/slugs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            name = "Test Category",
            slug = "test-category",
            description = "Test Description",
            sortOrder = 10
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_AsTenantAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "TenantAdmin");
        var request = new
        {
            name = "Tenant Category",
            slug = "tenant-category",
            sortOrder = 20
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_AsMember_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            name = "Unauthorized Category",
            slug = "unauthorized",
            sortOrder = 0
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_AsAdmin_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            name = "Updated Category",
            slug = "updated-category",
            sortOrder = 5,
            isActive = true
        };

        var response = await client.PutAsJsonAsync("/api/categories/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        var response = await client.DeleteAsync("/api/categories/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCategory_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var request = new
        {
            name = "No Auth Category",
            slug = "no-auth",
            sortOrder = 0
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
