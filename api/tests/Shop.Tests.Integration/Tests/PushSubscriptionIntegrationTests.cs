using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class PushSubscriptionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PushSubscriptionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetVapidKey_Returns200Or404()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/push/vapid-key");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Subscribe_Authenticated_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var subscription = new
        {
            Endpoint = "https://push.example.com/sub1",
            P256dh = "test-key",
            Auth = "test-auth"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/push/subscribe", subscription);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Subscribe_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var subscription = new
        {
            Endpoint = "https://push.example.com/sub1",
            P256dh = "test-key",
            Auth = "test-auth"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/push/subscribe", subscription);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unsubscribe_Authenticated_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var unsubscription = new
        {
            Endpoint = "https://push.example.com/sub1"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/push/unsubscribe", unsubscription);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unsubscribe_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var unsubscription = new
        {
            Endpoint = "https://push.example.com/sub1"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/push/unsubscribe", unsubscription);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
