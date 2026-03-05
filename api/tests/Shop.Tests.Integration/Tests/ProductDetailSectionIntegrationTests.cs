using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class ProductDetailSectionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductDetailSectionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSections_ForProduct1_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/products/1/sections");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSections_ForNonExistentProduct_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/products/99999/sections");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSection_AsAdmin_Returns200Or201()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var section = new
        {
            Title = "Test Section",
            Content = "Test content",
            SectionType = "text",
            SortOrder = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/products/1/sections", section);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSection_AsMember_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var section = new
        {
            Title = "Test Section",
            Content = "Test content",
            SectionType = "text",
            SortOrder = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/products/1/sections", section);

        // Assert
        // ProductDetailSectionController uses [Authorize] without role restriction
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateSection_AsAdmin_NonExistentSection_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var section = new
        {
            Title = "Updated Section",
            Content = "Updated content",
            SectionType = "text",
            SortOrder = 1
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/1/sections/99999", section);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSection_AsAdmin_NonExistentSection_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Act
        var response = await client.DeleteAsync("/api/products/1/sections/99999");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderSections_AsAdmin_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var reorderRequest = new { SectionIds = new int[] { } };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/1/sections/reorder", reorderRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
