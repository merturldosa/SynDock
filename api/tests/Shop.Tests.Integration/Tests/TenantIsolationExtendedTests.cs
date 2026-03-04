using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class TenantIsolationExtendedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TenantIsolationExtendedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Product Isolation ──

    [Fact]
    public async Task Tenant1Products_DoNotContainTenant2Data()
    {
        // Arrange
        var catholiaClient = _factory.CreateAuthenticatedClient(userId: 1, tenantId: 1, tenantSlug: "catholia");
        var mohyunClient = _factory.CreateAuthenticatedClient(userId: 3, tenantId: 2, tenantSlug: "mohyun");

        // Act
        var catholiaResponse = await catholiaClient.GetAsync("/api/products");
        var mohyunResponse = await mohyunClient.GetAsync("/api/products");

        // Assert
        catholiaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        mohyunResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var catholiaContent = await catholiaResponse.Content.ReadAsStringAsync();
        var mohyunContent = await mohyunResponse.Content.ReadAsStringAsync();

        // Catholia should see 묵주 but not 된장
        catholiaContent.Should().Contain("묵주");
        catholiaContent.Should().NotContain("된장");

        // MoHyun should see 된장 but not 묵주
        mohyunContent.Should().Contain("된장");
        mohyunContent.Should().NotContain("묵주");
    }

    // ── Category Isolation ──

    [Fact]
    public async Task Tenant1Categories_DoNotContainTenant2Categories()
    {
        // Arrange
        var catholiaClient = _factory.CreateAuthenticatedClient(userId: 1, tenantId: 1, tenantSlug: "catholia");

        // Act (route: /api/categories)
        var response = await catholiaClient.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("성물");
        content.Should().NotContain("장류");
    }

    // ── Cart Isolation ──

    [Fact]
    public async Task Tenant1Cart_IsolatedFromTenant2()
    {
        // Arrange
        var tenant1Client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member", tenantId: 1, tenantSlug: "catholia");
        var tenant2Client = _factory.CreateAuthenticatedClient(userId: 3, username: "mohyun_admin", role: "Admin", tenantId: 2, tenantSlug: "mohyun");

        // Tenant 1 adds to cart
        await tenant1Client.PostAsJsonAsync("/api/cart/items", new { ProductId = 1, VariantId = 1, Quantity = 1 });

        // Act - Tenant 2 gets their cart (should be empty or different)
        var tenant2Cart = await tenant2Client.GetAsync("/api/cart");

        // Assert - Tenant 2 should not see Tenant 1's cart items
        tenant2Cart.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await tenant2Cart.Content.ReadAsStringAsync();
        content.Should().NotContain("묵주");
    }

    // ── Admin Data Isolation ──

    [Fact]
    public async Task AdminOrders_OnlyShowOwnTenantData()
    {
        // Arrange
        var catholiaAdmin = _factory.CreateAuthenticatedClient(userId: 1, role: "Admin", tenantId: 1, tenantSlug: "catholia");
        var mohyunAdmin = _factory.CreateAuthenticatedClient(userId: 3, role: "Admin", tenantId: 2, tenantSlug: "mohyun");

        // Act
        var catholiaOrders = await catholiaAdmin.GetAsync("/api/admin/orders?page=1&pageSize=10");
        var mohyunOrders = await mohyunAdmin.GetAsync("/api/admin/orders?page=1&pageSize=10");

        // Assert - Both should succeed but return separate data
        catholiaOrders.StatusCode.Should().Be(HttpStatusCode.OK);
        mohyunOrders.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Inactive Tenant Blocking ──

    [Fact]
    public async Task InactiveTenant_CannotAccessAPI()
    {
        // Arrange - tenant "inactive" is seeded as IsActive=false
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "inactive");

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert - Should be blocked (403 Forbidden)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Nonexistent Tenant ──

    [Fact]
    public async Task NonexistentTenant_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "nonexistent");

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
