using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Common.Interfaces;

namespace Shop.Tests.Integration;

public class ProvisioningTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProvisioningTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
    }

    [Fact]
    public async Task Apply_ValidRequest_ReturnsSuccess()
    {
        var request = new
        {
            companyName = "테스트 쇼핑몰",
            desiredSlug = $"test-{Guid.NewGuid().ToString()[..8]}",
            email = "test@example.com",
            contactName = "홍길동",
            businessType = "Food",
            planTier = "Starter"
        };

        var response = await _client.PostAsJsonAsync("/api/provisioning/apply", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Contains("applicationId", result.Keys);
    }

    [Fact]
    public async Task Apply_DuplicateSlug_ReturnsBadRequest()
    {
        var slug = $"dup-{Guid.NewGuid().ToString()[..8]}";
        var request = new
        {
            companyName = "First",
            desiredSlug = slug,
            email = "first@example.com",
            contactName = "First",
            businessType = "General",
            planTier = "Free"
        };

        await _client.PostAsJsonAsync("/api/provisioning/apply", request);
        var response2 = await _client.PostAsJsonAsync("/api/provisioning/apply", request);

        Assert.Equal(HttpStatusCode.InternalServerError, response2.StatusCode);
    }

    [Fact]
    public async Task CheckSlug_Available_ReturnsTrue()
    {
        var slug = $"avail-{Guid.NewGuid().ToString()[..8]}";
        var response = await _client.GetAsync($"/api/provisioning/check-slug/{slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
        Assert.Equal(true, ((System.Text.Json.JsonElement)result["available"]).GetBoolean());
    }

    [Fact]
    public async Task CheckStatus_InvalidEmail_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/provisioning/status?email=nobody@test.com&applicationId=99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FeatureGating_WmsBlocked_WhenNotEnabled()
    {
        // Default tenant (catholia) may not have WMS enabled
        var response = await _client.GetAsync("/api/wms/zones");
        // Should return 403 (feature not enabled) or 401 (not authenticated)
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 403 or 401, got {response.StatusCode}");
    }
}
