using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class AdminAdvancedIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAdvancedIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // --- Dashboard ---

    [Fact]
    public async Task GetDashboardStats_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardStats_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Analytics ---

    [Fact]
    public async Task GetSalesAnalytics_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var response = await client.GetAsync($"/api/admin/analytics?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCustomerAnalytics_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/analytics/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductPerformance_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/analytics/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Admin Orders ---

    [Fact]
    public async Task GetAdminOrders_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/orders?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminOrders_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Stock Management ---

    [Fact]
    public async Task GetLowStock_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/low-stock");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Users ---

    [Fact]
    public async Task GetUsers_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/users?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- Tenant Settings ---

    [Fact]
    public async Task GetTenantSettings_AsTenantAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");
        var response = await client.GetAsync("/api/admin/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Billing & Settlements ---

    [Fact]
    public async Task GetMyBilling_AsTenantAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 4, username: "tenantadmin", role: "TenantAdmin");
        var response = await client.GetAsync("/api/admin/billing");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMySettlements_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/settlements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyCommissions_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/commissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Email Campaigns ---

    [Fact]
    public async Task GetCampaigns_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCampaignSummary_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/campaigns/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
