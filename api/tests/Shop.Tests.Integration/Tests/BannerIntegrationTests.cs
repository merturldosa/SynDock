using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class BannerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BannerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBanner_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            title = "Test Banner",
            description = "Test Description",
            imageUrl = "https://example.com/banner.jpg",
            linkUrl = "https://example.com",
            displayType = "Banner",
            pageTarget = "home",
            sortOrder = 1
        };

        var response = await client.PostAsJsonAsync("/api/banner", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveBanners_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/banner/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActiveBanners_WithPageFilter_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/banner/active?page=home");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllBanners_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        var response = await client.GetAsync("/api/banner?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateBanner_AsAdmin_Returns200OrNotFound()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Create first
        var createRequest = new
        {
            title = "Update Test",
            imageUrl = "https://example.com/banner.jpg",
            displayType = "Banner",
            sortOrder = 0
        };
        await client.PostAsJsonAsync("/api/banner", createRequest);

        // Update
        var updateRequest = new
        {
            title = "Updated Banner",
            imageUrl = "https://example.com/updated.jpg",
            displayType = "Popup",
            pageTarget = "products",
            sortOrder = 2,
            isActive = true
        };
        var response = await client.PutAsJsonAsync("/api/banner/1", updateRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteBanner_AsAdmin_Returns200OrNotFound()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        var response = await client.DeleteAsync("/api/banner/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBanner_AsRegularUser_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            title = "Unauthorized Banner",
            imageUrl = "https://example.com/banner.jpg",
            displayType = "Banner",
            sortOrder = 0
        };

        var response = await client.PostAsJsonAsync("/api/banner", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
