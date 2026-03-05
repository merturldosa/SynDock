using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Application.Admin.Commands;
using Shop.Application.Admin.Queries;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/admin/mes")]
[Authorize(Roles = "TenantAdmin,Admin,PlatformAdmin")]
public class MesIntegrationController : ControllerBase
{
    private readonly IMesClient _mesClient;
    private readonly IMesProductMapper _mapper;
    private readonly IShopDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IMediator _mediator;

    public MesIntegrationController(
        IMesClient mesClient,
        IMesProductMapper mapper,
        IShopDbContext db,
        IConfiguration configuration,
        IMediator mediator)
    {
        _mesClient = mesClient;
        _mapper = mapper;
        _db = db;
        _configuration = configuration;
        _mediator = mediator;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _mesClient.GetSyncStatusAsync();
        return Ok(status);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory(CancellationToken ct)
    {
        var inventory = await _mesClient.GetInventoryAsync();
        return Ok(inventory);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        var isAvailable = await _mesClient.IsAvailableAsync();
        if (!isAvailable)
            return BadRequest(new { message = "Cannot connect to MES server." });

        // The actual sync is done by the background job,
        // but we trigger an immediate check by returning current status
        var status = await _mesClient.GetSyncStatusAsync();
        return Ok(new { message = "Sync has been requested.", status });
    }

    [HttpGet("discrepancies")]
    public async Task<IActionResult> GetDiscrepancies(CancellationToken ct)
    {
        var mesInventory = await _mesClient.GetInventoryAsync();
        var mappings = await _mapper.GetAllMappingsAsync();

        var discrepancies = new List<MesStockDiscrepancy>();

        // Batch-load all needed data to avoid N+1 queries
        var productIds = mappings.Keys.ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var stockByProduct = await _db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(v => v.Stock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, ct);

        foreach (var mapping in mappings)
        {
            var mesItem = mesInventory.FirstOrDefault(i => i.ProductCode == mapping.Value);
            if (mesItem is null) continue;

            if (!products.TryGetValue(mapping.Key, out var product)) continue;

            var shopStock = stockByProduct.GetValueOrDefault(mapping.Key, 0);

            var mesStock = (int)Math.Round(mesItem.AvailableQuantity);
            var difference = shopStock - mesStock;
            if (difference != 0)
            {
                discrepancies.Add(new MesStockDiscrepancy(
                    mapping.Key, product.Name, mapping.Value,
                    shopStock, mesStock, difference));
            }
        }

        return Ok(discrepancies);
    }

    [HttpGet("inventory-comparison")]
    public async Task<IActionResult> GetInventoryComparison(CancellationToken ct)
    {
        var mesInventory = await _mesClient.GetInventoryAsync();
        var mappings = await _mapper.GetAllMappingsAsync();

        var comparisons = new List<MesInventoryComparison>();

        // Batch-load all needed data to avoid N+1 queries
        var productIds = mappings.Keys.ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var stockByProduct = await _db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(v => v.Stock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, ct);

        // 1) Shop 매핑 상품 순회
        foreach (var mapping in mappings)
        {
            if (!products.TryGetValue(mapping.Key, out var product)) continue;

            var shopStock = stockByProduct.GetValueOrDefault(mapping.Key, 0);

            var mesItem = mesInventory.FirstOrDefault(i => i.ProductCode == mapping.Value);
            var mesStock = mesItem is not null ? (int)Math.Round(mesItem.AvailableQuantity) : 0;
            var difference = shopStock - mesStock;

            string status;
            if (mesItem is null)
                status = "shop_only";
            else if (difference == 0)
                status = "matched";
            else
                status = "discrepancy";

            comparisons.Add(new MesInventoryComparison(
                mapping.Key, product.Name, mapping.Value,
                shopStock, mesStock, difference, status));
        }

        // 2) MES에만 있는 상품 (Shop에 매핑 없음)
        var mappedMesCodes = mappings.Values.ToHashSet();
        foreach (var mesItem in mesInventory)
        {
            if (mappedMesCodes.Contains(mesItem.ProductCode)) continue;

            comparisons.Add(new MesInventoryComparison(
                null, mesItem.ProductName ?? mesItem.ProductCode, mesItem.ProductCode,
                0, (int)Math.Round(mesItem.AvailableQuantity), -(int)Math.Round(mesItem.AvailableQuantity),
                "mes_only"));
        }

        return Ok(comparisons);
    }

    [HttpPost("sync-product/{productId:int}")]
    public async Task<IActionResult> SyncProduct(int productId, CancellationToken ct)
    {
        var mesCode = await _mapper.GetMesProductCodeAsync(productId);
        if (mesCode is null)
            return BadRequest(new { message = "Product has no MES mapping." });

        var mesInventory = await _mesClient.GetInventoryAsync();
        var mesItem = mesInventory.FirstOrDefault(i => i.ProductCode == mesCode);
        if (mesItem is null)
            return BadRequest(new { message = "Product inventory not found in MES." });

        var mesStock = (int)Math.Round(mesItem.AvailableQuantity);

        var variants = await _db.ProductVariants.IgnoreQueryFilters()
            .Where(v => v.ProductId == productId && v.IsActive)
            .ToListAsync(ct);

        if (variants.Count == 0)
            return BadRequest(new { message = "No product variants found in Shop." });

        // 대표 variant에 MES 재고 반영
        var primaryVariant = variants.First();
        primaryVariant.Stock = mesStock;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Inventory has been synced.", productId, mesStock });
    }

    [HttpGet("sync-history")]
    public async Task<IActionResult> GetSyncHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var query = _db.MesSyncHistories.AsNoTracking()
            .OrderByDescending(h => h.StartedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.StartedAt,
                h.CompletedAt,
                h.Status,
                h.SuccessCount,
                h.FailedCount,
                h.SkippedCount,
                h.ElapsedMs,
                h.ErrorDetailsJson,
                h.ConflictDetailsJson
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("sync-history/{id:int}")]
    public async Task<IActionResult> GetSyncHistoryDetail(int id, CancellationToken ct)
    {
        var item = await _db.MesSyncHistories.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        if (item is null)
            return NotFound(new { message = "Sync history not found." });

        return Ok(item);
    }

    [HttpPost("orders/{orderId:int}/forward")]
    public async Task<IActionResult> ForwardOrder(int orderId, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        var orderItems = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync(ct);

        var customerId = _configuration.GetValue<long>("Mes:CustomerId", 1);
        var salesUserId = _configuration.GetValue<long>("Mes:SalesUserId", 1);

        var lineNo = 0;
        var items = new List<MesSalesOrderLine>();
        foreach (var item in orderItems)
        {
            var mesCode = await _mapper.GetMesProductCodeAsync(item.ProductId);
            if (mesCode is null) continue;

            lineNo++;
            items.Add(new MesSalesOrderLine(
                LineNo: lineNo,
                ProductId: item.ProductId,
                OrderedQuantity: item.Quantity,
                Unit: "EA",
                UnitPrice: item.UnitPrice));
        }

        if (items.Count == 0)
            return BadRequest(new { message = "No MES-mapped products found." });

        var request = new MesSalesOrderRequest(
            OrderNo: order.OrderNumber,
            OrderDate: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            CustomerId: customerId,
            SalesUserId: salesUserId,
            Items: items);

        var result = await _mesClient.CreateSalesOrderAsync(request);

        if (result.Success)
        {
            // Save MES order ID back to Shop order
            var trackedOrder = await _db.Orders.FindAsync(orderId);
            if (trackedOrder is not null && result.MesOrderId is not null)
            {
                trackedOrder.MesOrderId = result.MesOrderId;
                await _db.SaveChangesAsync(ct);
            }
            return Ok(new { message = "Order has been forwarded to MES.", mesOrderId = result.MesOrderId });
        }
        return BadRequest(new { message = result.ErrorMessage });
    }

    // ── v2: Shop-MES Integration Bridge ──────────────────

    [HttpPost("inventory/reserve")]
    public async Task<IActionResult> ReserveInventory([FromBody] ReserveRequest request, CancellationToken ct)
    {
        var items = request.Items.Select(i => new MesReservationItem(i.ProductCode, i.Quantity)).ToList();
        var mesRequest = new MesReservationRequest(request.ShopOrderNo, Guid.NewGuid().ToString(), items);
        var result = await _mesClient.ReserveInventoryAsync(mesRequest);
        return Ok(result);
    }

    [HttpPost("inventory/release")]
    public async Task<IActionResult> ReleaseInventory([FromBody] ReserveRequest request, CancellationToken ct)
    {
        var items = request.Items.Select(i => new MesReservationItem(i.ProductCode, i.Quantity)).ToList();
        var mesRequest = new MesReservationRequest(request.ShopOrderNo, Guid.NewGuid().ToString(), items);
        var result = await _mesClient.ReleaseInventoryAsync(mesRequest);
        return Ok(result);
    }

    [HttpGet("orders/{orderId:int}/mes-status")]
    public async Task<IActionResult> GetMesOrderStatus(int orderId, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
            return NotFound(new { message = "Order not found." });

        var status = await _mesClient.GetOrderStatusAsync(order.OrderNumber);
        if (status is null)
            return Ok(new { message = "Order not found in MES.", shopOrderNo = order.OrderNumber });

        return Ok(status);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetMesProducts(CancellationToken ct)
    {
        var products = await _mesClient.GetMesProductsAsync();
        return Ok(products);
    }

    // ── Webhook: MES → Shop order status callback ────────

    [HttpPost("webhook/order-status")]
    [AllowAnonymous] // MES calls this via service-to-service auth
    public async Task<IActionResult> MesOrderStatusWebhook(
        [FromBody] MesOrderStatusWebhookPayload payload,
        [FromHeader(Name = "X-MES-Webhook-Secret")] string? webhookSecret,
        CancellationToken ct)
    {
        var expectedSecret = _configuration["Mes:WebhookSecret"];
        if (string.IsNullOrEmpty(expectedSecret))
            return StatusCode(503, new { message = "Webhook secret not configured" });
        if (webhookSecret != expectedSecret)
            return Unauthorized(new { message = "Invalid webhook secret" });

        if (string.IsNullOrEmpty(payload.ShopOrderNo))
            return BadRequest(new { message = "ShopOrderNo is required" });

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == payload.ShopOrderNo, ct);

        if (order is null)
            return NotFound(new { message = $"Shop order {payload.ShopOrderNo} not found" });

        // Update MES tracking fields
        if (payload.MesOrderId is not null)
            order.MesOrderId = payload.MesOrderId;
        if (payload.MesOrderNo is not null)
            order.MesOrderNo = payload.MesOrderNo;

        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Order status updated", shopOrderNo = payload.ShopOrderNo });
    }

    // ── L2: Production Plan Suggestions ────────────────────

    [HttpPost("production-plan/generate")]
    public async Task<IActionResult> GenerateProductionPlan([FromServices] IProductionPlanService planService, CancellationToken ct)
    {
        var suggestions = await planService.GenerateSuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpGet("production-plan")]
    public async Task<IActionResult> GetProductionPlanSuggestions([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = _db.ProductionPlanSuggestions.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var suggestions = await query
            .Select(s => new
            {
                s.Id, s.ProductId, s.ProductName, s.CurrentStock,
                s.AverageDailySales, s.EstimatedDaysUntilStockout,
                s.SuggestedQuantity, s.Urgency, s.Status,
                s.AiReason, s.TrendAnalysis, s.SeasonalityFactor, s.ConfidenceScore,
                s.MesOrderId, s.ApprovedAt, s.ApprovedBy, s.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(suggestions);
    }

    [HttpPut("production-plan/{id:int}/approve")]
    public async Task<IActionResult> ApproveProductionPlan(int id, [FromServices] IProductionPlanService planService, CancellationToken ct)
    {
        var result = await planService.ApproveSuggestionAsync(id, User.Identity?.Name ?? "Admin");
        if (result is null) return BadRequest(new { message = "Cannot approve in current status." });
        return Ok(result);
    }

    [HttpPut("production-plan/{id:int}/reject")]
    public async Task<IActionResult> RejectProductionPlan(int id, [FromBody] RejectRequest request, [FromServices] IProductionPlanService planService, CancellationToken ct)
    {
        var success = await planService.RejectSuggestionAsync(id, request.Reason);
        if (!success) return BadRequest(new { message = "Cannot reject in current status." });
        return Ok(new { success = true });
    }

    [HttpPost("production-plan/{id:int}/forward-mes")]
    public async Task<IActionResult> ForwardProductionPlanToMes(int id, [FromServices] IProductionPlanService planService, CancellationToken ct)
    {
        var mesOrderId = await planService.ForwardToMesAsync(id);
        if (mesOrderId is null) return BadRequest(new { message = "Failed to forward to MES." });
        return Ok(new { mesOrderId });
    }

    // ── L3: Auto Reorder ────────────────────────────────

    [HttpGet("auto-reorder/stats")]
    public async Task<IActionResult> GetAutoReorderStats(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAutoReorderStatsQuery(), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("auto-reorder/rules")]
    public async Task<IActionResult> GetAutoReorderRules([FromQuery] bool? enabledOnly = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAutoReorderRulesQuery(enabledOnly), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("auto-reorder/rules")]
    public async Task<IActionResult> UpsertAutoReorderRule([FromBody] UpsertAutoReorderRuleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpsertAutoReorderRuleCommand(
            request.ProductId, request.ReorderThreshold, request.ReorderQuantity,
            request.MaxStockLevel, request.IsEnabled, request.AutoForwardToMes,
            request.MinIntervalHours), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { ruleId = result.Data });
    }

    [HttpDelete("auto-reorder/rules/{id:int}")]
    public async Task<IActionResult> DeleteAutoReorderRule(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAutoReorderRuleCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("auto-reorder/rules/{id:int}/toggle")]
    public async Task<IActionResult> ToggleAutoReorderRule(int id, [FromBody] ToggleAutoReorderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleAutoReorderRuleCommand(id, request.IsEnabled), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("auto-reorder/rules/bulk")]
    public async Task<IActionResult> BulkCreateAutoReorderRules([FromBody] BulkCreateAutoReorderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new BulkCreateAutoReorderRulesCommand(
            request.ReorderThreshold, request.MinIntervalHours, request.AutoForwardToMes), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { createdCount = result.Data });
    }

    [HttpGet("purchase-orders")]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(status, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("purchase-orders/{id:int}/forward")]
    public async Task<IActionResult> ForwardPurchaseOrder(int id, CancellationToken ct)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (po is null) return NotFound(new { message = "Purchase order not found." });
        if (po.Status != "Created") return BadRequest(new { message = "Purchase order has already been forwarded." });

        var mesItems = po.Items.Where(i => !string.IsNullOrEmpty(i.MesProductCode)).ToList();
        if (mesItems.Count == 0) return BadRequest(new { message = "No MES-mapped products found." });

        var customerId = _configuration.GetValue<long>("Mes:CustomerId", 1);
        var salesUserId = _configuration.GetValue<long>("Mes:SalesUserId", 1);

        var lineNo = 0;
        var salesOrderLines = mesItems.Select(i => new MesSalesOrderLine(
            LineNo: ++lineNo,
            ProductId: i.ProductId,
            OrderedQuantity: i.OrderedQuantity,
            Unit: "EA",
            UnitPrice: 0
        )).ToList();

        var mesRequest = new MesSalesOrderRequest(
            OrderNo: po.OrderNumber,
            OrderDate: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            CustomerId: customerId,
            SalesUserId: salesUserId,
            Items: salesOrderLines);

        var mesResult = await _mesClient.CreateSalesOrderAsync(mesRequest);
        if (!mesResult.Success)
            return BadRequest(new { message = mesResult.ErrorMessage });

        po.Status = "Forwarded";
        po.MesOrderId = mesResult.MesOrderId;
        po.ForwardedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Forwarded to MES.", mesOrderId = mesResult.MesOrderId });
    }

    [HttpPost("purchase-orders/{id:int}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    // ── Request/Response DTOs ────────────────────────────

    public record ReserveRequest(string ShopOrderNo, List<ReserveItemRequest> Items);
    public record ReserveItemRequest(string ProductCode, decimal Quantity);
    public record RejectRequest(string Reason);
    public record MesOrderStatusWebhookPayload(
        string? ShopOrderNo, string? MesOrderId, string? MesOrderNo, string? Status, string? Message);
    public record UpsertAutoReorderRuleRequest(
        int ProductId, int ReorderThreshold, int ReorderQuantity = 0,
        int MaxStockLevel = 0, bool IsEnabled = true, bool AutoForwardToMes = true,
        int MinIntervalHours = 24);
    public record ToggleAutoReorderRequest(bool IsEnabled);
    public record BulkCreateAutoReorderRequest(int ReorderThreshold = 10, int MinIntervalHours = 24, bool AutoForwardToMes = true);
}
