using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class AuthFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
    }

    // ── Register Flow ──

    [Fact]
    public async Task Register_WithValidData_ReturnsTokens()
    {
        // Arrange
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@test.com";
        var request = new
        {
            Username = $"user_{Guid.NewGuid():N}"[..20],
            Email = uniqueEmail,
            Password = "NewUser123!",
            Name = "테스트 사용자"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
        content.User.Should().NotBeNull();
        content.User!.Email.Should().Be(uniqueEmail);
        content.User.Role.Should().Be("Member");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange - admin@catholia.com already seeded
        var request = new
        {
            Username = "duplicate_user",
            Email = "admin@catholia.com",
            Password = "Test123!",
            Name = "Duplicate"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    // ── Login → Refresh Token Flow ──

    [Fact]
    public async Task LoginThenRefresh_FullTokenLifecycle()
    {
        // Step 1: Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = "admin@catholia.com", Password = "Admin123!" });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.RequiresTwoFactor.Should().BeFalse();
        loginResult.Auth.Should().NotBeNull();
        loginResult.Auth!.RefreshToken.Should().NotBeNullOrEmpty();

        // Step 2: Use refresh token to get new access token
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = loginResult.Auth.RefreshToken });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();

        // Step 3: New access token should work on protected endpoint
        var meClient = _factory.CreateClient();
        meClient.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        meClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);

        var meResponse = await meClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsBadRequestOrUnauthorized()
    {
        // Arrange
        var request = new { RefreshToken = "invalid-refresh-token-value" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    // ── Register → Login → Access Protected Resource Flow ──

    [Fact]
    public async Task FullAuthFlow_RegisterLoginAccessProtected()
    {
        // Step 1: Register
        var uniqueEmail = $"flow_{Guid.NewGuid():N}@test.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = $"flowuser_{Guid.NewGuid():N}"[..20],
            Email = uniqueEmail,
            Password = "FlowTest123!",
            Name = "플로우 테스트"
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Step 2: Login with same credentials
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = uniqueEmail, Password = "FlowTest123!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Step 3: Access protected endpoint
        var protectedClient = _factory.CreateClient();
        protectedClient.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        protectedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Auth!.AccessToken);

        var meResponse = await protectedClient.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(uniqueEmail);
    }

    // ── DTOs ──
    private record LoginResponse(bool RequiresTwoFactor, string? TwoFactorToken, AuthResponse? Auth);
    private record AuthResponse(string? AccessToken, string? RefreshToken, UserResponse? User);
    private record UserResponse(int Id, string? Username, string? Email, string? Name, string? Phone, string? Role, string? CustomFieldsJson);
    private record UserProfileResponse(int Id, string? Username, string? Email, string? Name, string? Phone, string? Role);
}
