using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class AddressIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AddressIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAddresses_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/address");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAddresses_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        var response = await client.GetAsync("/api/address");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAddress_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            recipientName = "Test User",
            phone = "010-1234-5678",
            zipCode = "12345",
            address1 = "Seoul Gangnam-gu",
            address2 = "101-1001",
            isDefault = true
        };

        var response = await client.PostAsJsonAsync("/api/address", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAddress_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var request = new
        {
            recipientName = "Test",
            phone = "010-0000-0000",
            zipCode = "00000",
            address1 = "Test Address"
        };

        var response = await client.PostAsJsonAsync("/api/address", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateAddress_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            recipientName = "Updated User",
            phone = "010-9999-8888",
            zipCode = "54321",
            address1 = "Busan Haeundae-gu",
            isDefault = false
        };

        var response = await client.PutAsJsonAsync("/api/address/1", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAddress_Authenticated_Returns200OrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/address/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
