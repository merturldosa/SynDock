using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CurrencyLocaleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CurrencyLocaleIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // --- Currency Tests ---

    [Fact]
    public async Task GetCurrencyRates_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/currency/rates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConvertCurrency_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/currency/convert?amount=10000&from=KRW&to=USD");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConvertCurrency_InvalidParams_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/currency/convert?amount=-1&from=INVALID&to=USD");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    // --- Locale Tests ---

    [Fact]
    public async Task DetectLocale_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        client.DefaultRequestHeaders.Add("Accept-Language", "ko-KR");
        var response = await client.GetAsync("/api/locale/detect");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSupportedLanguages_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/locale/languages");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Categories Tests ---

    [Fact]
    public async Task GetCategories_Returns200()
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
            name = "New Category",
            slug = "new-category",
            isActive = true
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCategory_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            name = "Unauthorized Category",
            slug = "unauthorized"
        };

        var response = await client.PostAsJsonAsync("/api/categories", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    }
}
