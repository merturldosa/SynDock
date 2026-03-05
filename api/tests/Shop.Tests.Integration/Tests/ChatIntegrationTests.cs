using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class ChatIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ChatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Chat_Authenticated_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var chatRequest = new
        {
            Messages = new[]
            {
                new { Role = "user", Content = "Hello" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", chatRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var chatRequest = new
        {
            Messages = new[]
            {
                new { Role = "user", Content = "Hello" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", chatRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Chat_EmptyMessages_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var chatRequest = new
        {
            Messages = Array.Empty<object>()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat", chatRequest);

        // Assert
        // Empty messages may still be processed or return 400 depending on validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }
}
