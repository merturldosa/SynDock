using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class PlatformFinancialIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PlatformFinancialIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Tenant Creation ──

    [Fact]
    public async Task CreateTenant_AsPlatformAdmin_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");
        var request = new
        {
            Name = "Test Tenant",
            Slug = $"test-{Guid.NewGuid():N}"[..20],
            Subdomain = $"test{Guid.NewGuid():N}"[..15]
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/platform/tenants", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTenant_AsAdmin_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 1, role: "Admin");
        var request = new
        {
            Name = "Unauthorized Tenant",
            Slug = "unauthorized",
            Subdomain = "unauthorized"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/platform/tenants", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Invoices ──

    [Fact]
    public async Task GetTenantInvoices_AsPlatformAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants/catholia/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Commissions ──

    [Fact]
    public async Task GetTenantCommissions_AsPlatformAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants/catholia/commissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Settlements ──

    [Fact]
    public async Task GetSettlements_AsPlatformAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants/catholia/settlements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateSettlement_AsPlatformAdmin_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");
        var request = new
        {
            StartDate = DateTime.UtcNow.AddDays(-30).ToString("o"),
            EndDate = DateTime.UtcNow.ToString("o")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/platform/tenants/catholia/settlements", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }
}
