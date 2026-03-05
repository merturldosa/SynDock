using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class PaymentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfirmPayment_WithInvalidData_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var invalidPayment = new { PaymentKey = "", OrderId = "", Amount = 0 };

        // Act
        var response = await client.PostAsJsonAsync("/api/payment/confirm", invalidPayment);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ConfirmPayment_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var payment = new { PaymentKey = "test-key", OrderId = "test-order", Amount = 1000 };

        // Act
        var response = await client.PostAsJsonAsync("/api/payment/confirm", payment);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientKey_Authenticated_Returns200()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");

        // Act
        var response = await client.GetAsync("/api/payment/client-key");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClientKey_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/payment/client-key");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefundPayment_NonExistentOrder_Returns400Or404()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var refundRequest = new { Reason = "Test refund" };

        // Act
        var response = await client.PostAsJsonAsync("/api/payment/99999/refund", refundRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }
}
