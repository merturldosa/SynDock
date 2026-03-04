using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class RoleAuthorizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoleAuthorizationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Admin Controller Access (using /api/admin/users which works with InMemory DB) ──

    [Fact]
    public async Task AdminUsers_AsAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 1, role: "Admin");

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUsers_AsTenantAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUsers_AsPlatformAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminUsers_AsMember_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Platform Controller Access ──

    [Fact]
    public async Task PlatformTenants_AsPlatformAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 5, username: "platformadmin", role: "PlatformAdmin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlatformTenants_AsAdmin_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 1, role: "Admin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformTenants_AsTenantAdmin_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");

        // Act
        var response = await client.GetAsync("/api/platform/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PlatformTenants_AsMember_Returns403()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Act
        var response = await client.GetAsync("/api/platform/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Admin Stats (InMemory DB compatibility) ──

    [Fact]
    public async Task AdminStats_AsAdmin_Returns200()
    {
        // Arrange - This tests the fixed GetDashboardStatsQuery with InMemory DB
        var client = _factory.CreateAuthenticatedClient(userId: 1, role: "Admin");

        // Act
        var response = await client.GetAsync("/api/admin/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Admin Orders (role boundary) ──

    [Fact]
    public async Task AdminOrders_AsTenantAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");

        // Act
        var response = await client.GetAsync("/api/admin/orders?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminAnalytics_AsTenantAdmin_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");

        // Act
        var response = await client.GetAsync("/api/admin/analytics?days=7");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Role Hierarchy Verification (using /api/admin/users) ──

    [Theory]
    [InlineData("PlatformAdmin", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.OK)]
    [InlineData("TenantAdmin", HttpStatusCode.OK)]
    [InlineData("Member", HttpStatusCode.Forbidden)]
    public async Task AdminEndpoint_RoleHierarchy(string role, HttpStatusCode expectedStatus)
    {
        // Arrange
        var userId = role switch
        {
            "PlatformAdmin" => 5,
            "Admin" => 1,
            "TenantAdmin" => 4,
            _ => 2
        };
        var client = _factory.CreateAuthenticatedClient(userId: userId, role: role);

        // Act
        var response = await client.GetAsync("/api/admin/users");

        // Assert
        response.StatusCode.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("PlatformAdmin", HttpStatusCode.OK)]
    [InlineData("Admin", HttpStatusCode.Forbidden)]
    [InlineData("TenantAdmin", HttpStatusCode.Forbidden)]
    [InlineData("Member", HttpStatusCode.Forbidden)]
    public async Task PlatformEndpoint_OnlyPlatformAdmin(string role, HttpStatusCode expectedStatus)
    {
        // Arrange
        var userId = role switch
        {
            "PlatformAdmin" => 5,
            "Admin" => 1,
            "TenantAdmin" => 4,
            _ => 2
        };
        var client = _factory.CreateAuthenticatedClient(userId: userId, role: role);

        // Act
        var response = await client.GetAsync("/api/platform/tenants");

        // Assert
        response.StatusCode.Should().Be(expectedStatus);
    }
}
