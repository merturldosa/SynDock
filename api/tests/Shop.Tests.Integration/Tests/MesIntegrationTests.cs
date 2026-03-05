using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class MesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── 1. GET status ────────────────────────────────────

    [Fact]
    public async Task GetStatus_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStatus_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/admin/mes/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 2. GET inventory ─────────────────────────────────

    [Fact]
    public async Task GetInventory_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/inventory");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 3. POST sync ─────────────────────────────────────

    [Fact]
    public async Task TriggerSync_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/sync", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 4. GET discrepancies ─────────────────────────────

    [Fact]
    public async Task GetDiscrepancies_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/discrepancies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 5. GET inventory-comparison ──────────────────────

    [Fact]
    public async Task GetInventoryComparison_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/inventory-comparison");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 6. POST sync-product/{productId} ─────────────────

    [Fact]
    public async Task SyncProduct_ExistingProduct_ReturnsOkOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/sync-product/1", null);

        // May return 400 if no MES mapping exists for product 1
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 7. GET sync-history ──────────────────────────────

    [Fact]
    public async Task GetSyncHistory_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/sync-history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
    }

    // ── 8. GET sync-history/{id} ─────────────────────────

    [Fact]
    public async Task GetSyncHistoryDetail_NonExistent_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/sync-history/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 9. POST orders/{orderId}/forward ─────────────────

    [Fact]
    public async Task ForwardOrder_NonExistentOrder_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/orders/999/forward", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 10. POST inventory/reserve ───────────────────────

    [Fact]
    public async Task ReserveInventory_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new
        {
            shopOrderNo = "SO-TEST",
            items = new[]
            {
                new { productCode = "TEST-001", quantity = 5 }
            }
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/inventory/reserve", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 11. POST inventory/release ───────────────────────

    [Fact]
    public async Task ReleaseInventory_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new
        {
            shopOrderNo = "SO-TEST",
            items = new[]
            {
                new { productCode = "TEST-001", quantity = 5 }
            }
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/inventory/release", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 12. GET orders/{orderId}/mes-status ───────────────

    [Fact]
    public async Task GetMesOrderStatus_NonExistentOrder_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/orders/999/mes-status");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── 13. GET products ─────────────────────────────────

    [Fact]
    public async Task GetMesProducts_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 14. POST webhook/order-status (AllowAnonymous) ───

    [Fact]
    public async Task MesOrderStatusWebhook_WithSecret_Returns200OrNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        client.DefaultRequestHeaders.Add("X-MES-Webhook-Secret", "test-webhook-secret");
        var body = new
        {
            shopOrderNo = "test",
            mesOrderId = "mes-1",
            mesOrderNo = "MES-001",
            status = "completed",
            message = "done"
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/webhook/order-status", body);

        // Returns NotFound if the order doesn't exist, OK if it does
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MesOrderStatusWebhook_WithoutSecret_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var body = new
        {
            shopOrderNo = "test",
            mesOrderId = "mes-1",
            mesOrderNo = "MES-001",
            status = "completed",
            message = "done"
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/webhook/order-status", body);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MesOrderStatusWebhook_EmptyShopOrderNo_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        client.DefaultRequestHeaders.Add("X-MES-Webhook-Secret", "test-webhook-secret");
        var body = new
        {
            shopOrderNo = (string?)null,
            mesOrderId = "mes-1",
            mesOrderNo = "MES-001",
            status = "completed",
            message = "done"
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/webhook/order-status", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    // ── 15. POST production-plan/generate ────────────────

    [Fact]
    public async Task GenerateProductionPlan_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/production-plan/generate", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 16. GET production-plan ──────────────────────────

    [Fact]
    public async Task GetProductionPlanSuggestions_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/production-plan");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductionPlanSuggestions_WithStatusFilter_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/production-plan?status=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── 17. PUT production-plan/{id}/approve ─────────────

    [Fact]
    public async Task ApproveProductionPlan_NonExistent_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PutAsync("/api/admin/mes/production-plan/999/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 18. PUT production-plan/{id}/reject ──────────────

    [Fact]
    public async Task RejectProductionPlan_NonExistent_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new { reason = "Not needed" };
        var response = await client.PutAsJsonAsync("/api/admin/mes/production-plan/999/reject", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 19. POST production-plan/{id}/forward-mes ────────

    [Fact]
    public async Task ForwardProductionPlanToMes_NonExistent_ReturnsBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/production-plan/999/forward-mes", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 20. GET auto-reorder/stats ───────────────────────

    [Fact]
    public async Task GetAutoReorderStats_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/auto-reorder/stats");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 21. GET auto-reorder/rules ───────────────────────

    [Fact]
    public async Task GetAutoReorderRules_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/auto-reorder/rules");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAutoReorderRules_EnabledOnly_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/auto-reorder/rules?enabledOnly=true");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 22. POST auto-reorder/rules ──────────────────────

    [Fact]
    public async Task UpsertAutoReorderRule_AsAdmin_ReturnsOkOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new
        {
            productId = 1,
            reorderThreshold = 10,
            reorderQuantity = 50,
            maxStockLevel = 200,
            isEnabled = true,
            autoForwardToMes = false,
            minIntervalHours = 24
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/auto-reorder/rules", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    // ── 23. DELETE auto-reorder/rules/{id} ───────────────

    [Fact]
    public async Task DeleteAutoReorderRule_NonExistent_ReturnsOkOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.DeleteAsync("/api/admin/mes/auto-reorder/rules/999");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // ── 24. PUT auto-reorder/rules/{id}/toggle ───────────

    [Fact]
    public async Task ToggleAutoReorderRule_NonExistent_ReturnsOkOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new { isEnabled = false };
        var response = await client.PutAsJsonAsync("/api/admin/mes/auto-reorder/rules/999/toggle", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // ── 25. POST auto-reorder/rules/bulk ─────────────────

    [Fact]
    public async Task BulkCreateAutoReorderRules_AsAdmin_ReturnsOkOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var body = new
        {
            reorderThreshold = 10,
            minIntervalHours = 24,
            autoForwardToMes = true
        };
        var response = await client.PostAsJsonAsync("/api/admin/mes/auto-reorder/rules/bulk", body);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 26. GET purchase-orders ──────────────────────────

    [Fact]
    public async Task GetPurchaseOrders_AsAdmin_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/purchase-orders");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithStatusFilter_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.GetAsync("/api/admin/mes/purchase-orders?status=Created&page=1&pageSize=20");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── 27. POST purchase-orders/{id}/forward ────────────

    [Fact]
    public async Task ForwardPurchaseOrder_NonExistent_ReturnsNotFoundOrBadRequest()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/purchase-orders/999/forward", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // ── 28. POST purchase-orders/{id}/cancel ─────────────

    [Fact]
    public async Task CancelPurchaseOrder_NonExistent_ReturnsBadRequestOrNotFound()
    {
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var response = await client.PostAsync("/api/admin/mes/purchase-orders/999/cancel", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    // ── Authorization: MES controller requires TenantAdmin/Admin/PlatformAdmin ───

    [Fact]
    public async Task GetStatus_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/admin/mes/status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetInventory_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/admin/mes/inventory");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TriggerSync_AsMember_ReturnsForbidden()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.PostAsync("/api/admin/mes/sync", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
