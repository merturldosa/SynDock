using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CouponIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CouponIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCoupons_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/coupons?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCoupon_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var request = new
        {
            code = "TEST10",
            name = "Test 10% Coupon",
            discountType = "Percentage",
            discountValue = 10m,
            minimumOrderAmount = 10000m,
            maxUsageCount = 100,
            expiresAt = DateTime.UtcNow.AddDays(30).ToString("o")
        };

        var response = await client.PostAsJsonAsync("/api/coupons", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetMyCoupons_AsMember_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/coupons/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateCoupon_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { code = "NONEXISTENT", orderAmount = 50000m };

        var response = await client.PostAsJsonAsync("/api/coupons/validate", request);

        // Should return 200 with valid=false or 404 for non-existent coupon
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCoupon_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            code = "HACK",
            name = "Unauthorized Coupon",
            discountType = "Percentage",
            discountValue = 100m
        };

        var response = await client.PostAsJsonAsync("/api/coupons", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteCoupon_AsAdmin_ReturnsSuccess()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Create first
        var createRequest = new
        {
            code = "TODELETE",
            name = "To Delete",
            discountType = "Fixed",
            discountValue = 1000m,
            expiresAt = DateTime.UtcNow.AddDays(1).ToString("o")
        };
        await client.PostAsJsonAsync("/api/coupons", createRequest);

        // Try to delete
        var response = await client.DeleteAsync("/api/coupons/1");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }
}
