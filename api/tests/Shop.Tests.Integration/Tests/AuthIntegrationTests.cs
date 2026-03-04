using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var loginRequest = new { Email = "admin@catholia.com", Password = "Admin123!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.RequiresTwoFactor.Should().BeFalse();
        content.Auth.Should().NotBeNull();
        content.Auth!.AccessToken.Should().NotBeNullOrEmpty();
        content.Auth.RefreshToken.Should().NotBeNullOrEmpty();
        content.Auth.User.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        // Arrange
        var loginRequest = new { Email = "admin@catholia.com", Password = "WrongPassword!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        // Arrange
        var authenticatedClient = _factory.CreateAuthenticatedClient();

        // Act
        var response = await authenticatedClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record LoginResponse(bool RequiresTwoFactor, string? TwoFactorToken, AuthResponse? Auth);
    private record AuthResponse(string? AccessToken, string? RefreshToken, UserResponse? User);
    private record UserResponse(int Id, string? Username, string? Email, string? Name, string? Phone, string? Role, string? CustomFieldsJson);
}
