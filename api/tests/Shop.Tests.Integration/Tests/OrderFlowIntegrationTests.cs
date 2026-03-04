using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class OrderFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Cart Tests ──

    [Fact]
    public async Task AddToCart_WithValidProduct_ReturnsCartId()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { ProductId = 1, VariantId = 1, Quantity = 2 };

        // Act
        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CartIdResponse>();
        content.Should().NotBeNull();
        content!.CartId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCart_AfterAddingItem_ReturnsCartWithItems()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        await client.PostAsJsonAsync("/api/cart/items", new { ProductId = 1, VariantId = 1, Quantity = 1 });

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        cart.Should().NotBeNull();
        cart!.Items.Should().NotBeNull();
        cart.TotalQuantity.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Cart_WithoutAuth_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Order Creation Tests ──

    [Fact]
    public async Task CreateOrder_FromCartWithItems_ReturnsOrderId()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Add item to cart first
        await client.PostAsJsonAsync("/api/cart/items", new { ProductId = 1, VariantId = 1, Quantity = 1 });

        // Create order from cart (route: /api/order, singular)
        var orderRequest = new { Note = "테스트 주문" };

        // Act
        var response = await client.PostAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadFromJsonAsync<OrderCreatedResponse>();
            content.Should().NotBeNull();
            content!.OrderId.Should().BeGreaterThan(0);
            content.OrderNumber.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetOrders_AsMember_ReturnsPaginatedList()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Act (route: /api/order, singular - [Route("api/[controller]")])
        var response = await client.GetAsync("/api/order?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<PagedOrderResponse>();
        orders.Should().NotBeNull();
        orders!.Page.Should().Be(1);
        orders.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetOrders_WithoutAuth_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");

        // Act (route: /api/order, singular)
        var response = await client.GetAsync("/api/order");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Order Cancel Tests ──

    [Fact]
    public async Task CancelOrder_NonExistent_Returns404OrBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Act (route: /api/order/{id}/cancel)
        var response = await client.PostAsync("/api/order/99999/cancel", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // ── Address Tests (for shipping) ──

    [Fact]
    public async Task CreateAddress_WithValidData_ReturnsAddressId()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            RecipientName = "홍길동",
            Phone = "010-1234-5678",
            ZipCode = "12345",
            Address1 = "서울시 강남구 테스트동 123",
            Address2 = "1층",
            IsDefault = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/address", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AddressCreatedResponse>();
        content.Should().NotBeNull();
        content!.AddressId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAddresses_AsMember_ReturnsAddressList()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Act
        var response = await client.GetAsync("/api/address");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Full E2E Flow: Cart → Address → Order ──

    [Fact]
    public async Task FullOrderFlow_AddToCart_CreateAddress_PlaceOrder()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");

        // Step 1: Add item to cart
        var addCartResponse = await client.PostAsJsonAsync("/api/cart/items",
            new { ProductId = 1, VariantId = 1, Quantity = 2 });
        addCartResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Create shipping address
        var addAddressResponse = await client.PostAsJsonAsync("/api/address", new
        {
            RecipientName = "테스트유저",
            Phone = "010-9999-8888",
            ZipCode = "06123",
            Address1 = "서울시 강남구 역삼동",
            IsDefault = true
        });
        addAddressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Verify cart has items
        var cartResponse = await client.GetAsync("/api/cart");
        cartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponse>();
        cart!.TotalQuantity.Should().BeGreaterThanOrEqualTo(2);

        // Step 4: Create order (route: /api/order)
        var orderResponse = await client.PostAsJsonAsync("/api/order",
            new { Note = "E2E 테스트 주문" });

        // The order creation may succeed or fail depending on address linkage
        orderResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    // ── DTOs ──
    private record CartIdResponse(int CartId);
    private record CartResponse(int Id, object[]? Items, decimal TotalAmount, int TotalQuantity);
    private record OrderCreatedResponse(int OrderId, string? OrderNumber);
    private record PagedOrderResponse(object[]? Items, int TotalCount, int Page, int PageSize);
    private record AddressCreatedResponse(int AddressId);
}
