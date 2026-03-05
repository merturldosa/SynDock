using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class AuthFlowExtendedIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowExtendedIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
    }

    // ── Forgot Password ──

    [Fact]
    public async Task ForgotPassword_WithValidEmail_Returns200()
    {
        // Arrange
        var request = new { Email = "admin@catholia.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithEmptyEmail_Returns200()
    {
        // Arrange - always returns 200 to prevent email enumeration
        var request = new { Email = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Reset Password ──

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        // Arrange
        var request = new
        {
            Token = "invalid-reset-token",
            NewPassword = "NewPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Email Verification ──

    [Fact]
    public async Task SendVerification_Returns200Or400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsync("/api/auth/send-verification", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_Returns400()
    {
        // Arrange
        var request = new { Token = "invalid-verification-token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Profile Update ──

    [Fact]
    public async Task UpdateProfile_Authenticated_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new { Name = "Updated Admin" };

        // Act
        var response = await client.PutAsJsonAsync("/api/auth/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Change Password ──

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new
        {
            CurrentPassword = "WrongPassword!",
            NewPassword = "NewPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/change-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 2FA ──

    [Fact]
    public async Task Enable2FA_Authenticated_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsync("/api/auth/2fa/enable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
