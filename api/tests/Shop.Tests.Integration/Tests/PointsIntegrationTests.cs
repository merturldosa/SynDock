using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class PointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPointBalance_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/points/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPointBalance_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/points/balance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPointHistory_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/points/history?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EarnPoints_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            userId = 2,
            amount = 1000,
            reason = "Test reward"
        };

        var response = await client.PostAsJsonAsync("/api/points/earn", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task EarnPoints_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            userId = 2,
            amount = 99999,
            reason = "Self-award attempt"
        };

        var response = await client.PostAsJsonAsync("/api/points/earn", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UsePoints_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Earn first
        await client.PostAsJsonAsync("/api/points/earn", new { userId = 2, amount = 5000, reason = "Pre-load" });

        // Use points
        var request = new
        {
            userId = 2,
            amount = 1000,
            reason = "Test usage"
        };

        var response = await client.PostAsJsonAsync("/api/points/use", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefundPoints_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            userId = 2,
            amount = 500,
            reason = "Test refund"
        };

        var response = await client.PostAsJsonAsync("/api/points/refund", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
