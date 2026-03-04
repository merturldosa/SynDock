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
[Authorize]
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
    public async Task<IActionResult> GetStatus()
    {
        var status = await _mesClient.GetSyncStatusAsync();
        return Ok(status);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var inventory = await _mesClient.GetInventoryAsync();
        return Ok(inventory);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync()
    {
        var isAvailable = await _mesClient.IsAvailableAsync();
        if (!isAvailable)
            return BadRequest(new { message = "MES 서버에 연결할 수 없습니다." });

        // The actual sync is done by the background job,
        // but we trigger an immediate check by returning current status
        var status = await _mesClient.GetSyncStatusAsync();
        return Ok(new { message = "동기화가 요청되었습니다.", status });
    }

    [HttpGet("discrepancies")]
    public async Task<IActionResult> GetDiscrepancies()
    {
        var mesInventory = await _mesClient.GetInventoryAsync();
        var mappings = await _mapper.GetAllMappingsAsync();

        var discrepancies = new List<MesStockDiscrepancy>();

        foreach (var mapping in mappings)
        {
            var mesItem = mesInventory.FirstOrDefault(i => i.ProductCode == mapping.Value);
            if (mesItem is null) continue;

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == mapping.Key);
            if (product is null) continue;

            var shopStock = await _db.ProductVariants.AsNoTracking()
                .Where(v => v.ProductId == mapping.Key && v.IsActive)
                .SumAsync(v => v.Stock);

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
    public async Task<IActionResult> GetInventoryComparison()
    {
        var mesInventory = await _mesClient.GetInventoryAsync();
        var mappings = await _mapper.GetAllMappingsAsync();

        var comparisons = new List<MesInventoryComparison>();

        // 1) Shop 매핑 상품 순회
        foreach (var mapping in mappings)
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == mapping.Key);
            if (product is null) continue;

            var shopStock = await _db.ProductVariants.AsNoTracking()
                .Where(v => v.ProductId == mapping.Key && v.IsActive)
                .SumAsync(v => v.Stock);

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
    public async Task<IActionResult> SyncProduct(int productId)
    {
        var mesCode = await _mapper.GetMesProductCodeAsync(productId);
        if (mesCode is null)
            return BadRequest(new { message = "MES 매핑이 없는 상품입니다." });

        var mesInventory = await _mesClient.GetInventoryAsync();
        var mesItem = mesInventory.FirstOrDefault(i => i.ProductCode == mesCode);
        if (mesItem is null)
            return BadRequest(new { message = "MES에서 해당 상품의 재고를 찾을 수 없습니다." });

        var mesStock = (int)Math.Round(mesItem.AvailableQuantity);

        var variants = await _db.ProductVariants
            .Where(v => v.ProductId == productId && v.IsActive)
            .ToListAsync();

        if (variants.Count == 0)
            return BadRequest(new { message = "Shop에 해당 상품의 옵션이 없습니다." });

        // 대표 variant에 MES 재고 반영
        var primaryVariant = variants.First();
        primaryVariant.Stock = mesStock;
        await _db.SaveChangesAsync();

        return Ok(new { message = "재고가 동기화되었습니다.", productId, mesStock });
    }

    [HttpGet("sync-history")]
    public async Task<IActionResult> GetSyncHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.MesSyncHistories.AsNoTracking()
            .OrderByDescending(h => h.StartedAt);

        var total = await query.CountAsync();
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
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("sync-history/{id:int}")]
    public async Task<IActionResult> GetSyncHistoryDetail(int id)
    {
        var item = await _db.MesSyncHistories.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (item is null)
            return NotFound(new { message = "동기화 이력을 찾을 수 없습니다." });

        return Ok(item);
    }

    [HttpPost("orders/{orderId:int}/forward")]
    public async Task<IActionResult> ForwardOrder(int orderId)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return NotFound(new { message = "주문을 찾을 수 없습니다." });

        var orderItems = await _db.OrderItems.AsNoTracking()
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync();

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
            return BadRequest(new { message = "MES 매핑된 상품이 없습니다." });

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
                await _db.SaveChangesAsync();
            }
            return Ok(new { message = "주문이 MES로 전달되었습니다.", mesOrderId = result.MesOrderId });
        }
        return BadRequest(new { message = result.ErrorMessage });
    }

    // ── v2: Shop-MES Integration Bridge ──────────────────

    [HttpPost("inventory/reserve")]
    public async Task<IActionResult> ReserveInventory([FromBody] ReserveRequest request)
    {
        var items = request.Items.Select(i => new MesReservationItem(i.ProductCode, i.Quantity)).ToList();
        var mesRequest = new MesReservationRequest(request.ShopOrderNo, Guid.NewGuid().ToString(), items);
        var result = await _mesClient.ReserveInventoryAsync(mesRequest);
        return Ok(result);
    }

    [HttpPost("inventory/release")]
    public async Task<IActionResult> ReleaseInventory([FromBody] ReserveRequest request)
    {
        var items = request.Items.Select(i => new MesReservationItem(i.ProductCode, i.Quantity)).ToList();
        var mesRequest = new MesReservationRequest(request.ShopOrderNo, Guid.NewGuid().ToString(), items);
        var result = await _mesClient.ReleaseInventoryAsync(mesRequest);
        return Ok(result);
    }

    [HttpGet("orders/{orderId:int}/mes-status")]
    public async Task<IActionResult> GetMesOrderStatus(int orderId)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return NotFound(new { message = "주문을 찾을 수 없습니다." });

        var status = await _mesClient.GetOrderStatusAsync(order.OrderNumber);
        if (status is null)
            return Ok(new { message = "MES에 해당 주문이 없습니다.", shopOrderNo = order.OrderNumber });

        return Ok(status);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetMesProducts()
    {
        var products = await _mesClient.GetMesProductsAsync();
        return Ok(products);
    }

    // ── Webhook: MES → Shop order status callback ────────

    [HttpPost("webhook/order-status")]
    [AllowAnonymous] // MES calls this via service-to-service auth
    public async Task<IActionResult> MesOrderStatusWebhook(
        [FromBody] MesOrderStatusWebhookPayload payload,
        [FromHeader(Name = "X-MES-Webhook-Secret")] string? webhookSecret)
    {
        var expectedSecret = _configuration["Mes:WebhookSecret"];
        if (!string.IsNullOrEmpty(expectedSecret) && webhookSecret != expectedSecret)
            return Unauthorized(new { message = "Invalid webhook secret" });

        if (string.IsNullOrEmpty(payload.ShopOrderNo))
            return BadRequest(new { message = "ShopOrderNo is required" });

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == payload.ShopOrderNo);

        if (order is null)
            return NotFound(new { message = $"Shop order {payload.ShopOrderNo} not found" });

        // Update MES tracking fields
        if (payload.MesOrderId is not null)
            order.MesOrderId = payload.MesOrderId;
        if (payload.MesOrderNo is not null)
            order.MesOrderNo = payload.MesOrderNo;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Order status updated", shopOrderNo = payload.ShopOrderNo });
    }

    // ── L2: Production Plan Suggestions ────────────────────

    [HttpPost("production-plan/generate")]
    public async Task<IActionResult> GenerateProductionPlan([FromServices] IProductionPlanService planService)
    {
        var suggestions = await planService.GenerateSuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpGet("production-plan")]
    public async Task<IActionResult> GetProductionPlanSuggestions([FromQuery] string? status = null)
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
            .ToListAsync();

        return Ok(suggestions);
    }

    [HttpPut("production-plan/{id:int}/approve")]
    public async Task<IActionResult> ApproveProductionPlan(int id, [FromServices] IProductionPlanService planService)
    {
        var result = await planService.ApproveSuggestionAsync(id, User.Identity?.Name ?? "Admin");
        if (result is null) return BadRequest(new { message = "승인할 수 없는 상태입니다." });
        return Ok(result);
    }

    [HttpPut("production-plan/{id:int}/reject")]
    public async Task<IActionResult> RejectProductionPlan(int id, [FromBody] RejectRequest request, [FromServices] IProductionPlanService planService)
    {
        var success = await planService.RejectSuggestionAsync(id, request.Reason);
        if (!success) return BadRequest(new { message = "거절할 수 없는 상태입니다." });
        return Ok(new { success = true });
    }

    [HttpPost("production-plan/{id:int}/forward-mes")]
    public async Task<IActionResult> ForwardProductionPlanToMes(int id, [FromServices] IProductionPlanService planService)
    {
        var mesOrderId = await planService.ForwardToMesAsync(id);
        if (mesOrderId is null) return BadRequest(new { message = "MES 전송에 실패했습니다." });
        return Ok(new { mesOrderId });
    }

    // ── L3: Auto Reorder ────────────────────────────────

    [HttpGet("auto-reorder/stats")]
    public async Task<IActionResult> GetAutoReorderStats()
    {
        var result = await _mediator.Send(new GetAutoReorderStatsQuery());
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpGet("auto-reorder/rules")]
    public async Task<IActionResult> GetAutoReorderRules([FromQuery] bool? enabledOnly = null)
    {
        var result = await _mediator.Send(new GetAutoReorderRulesQuery(enabledOnly));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("auto-reorder/rules")]
    public async Task<IActionResult> UpsertAutoReorderRule([FromBody] UpsertAutoReorderRuleRequest request)
    {
        var result = await _mediator.Send(new UpsertAutoReorderRuleCommand(
            request.ProductId, request.ReorderThreshold, request.ReorderQuantity,
            request.MaxStockLevel, request.IsEnabled, request.AutoForwardToMes,
            request.MinIntervalHours));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { ruleId = result.Data });
    }

    [HttpDelete("auto-reorder/rules/{id:int}")]
    public async Task<IActionResult> DeleteAutoReorderRule(int id)
    {
        var result = await _mediator.Send(new DeleteAutoReorderRuleCommand(id));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPut("auto-reorder/rules/{id:int}/toggle")]
    public async Task<IActionResult> ToggleAutoReorderRule(int id, [FromBody] ToggleAutoReorderRequest request)
    {
        var result = await _mediator.Send(new ToggleAutoReorderRuleCommand(id, request.IsEnabled));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { success = true });
    }

    [HttpPost("auto-reorder/rules/bulk")]
    public async Task<IActionResult> BulkCreateAutoReorderRules([FromBody] BulkCreateAutoReorderRequest request)
    {
        var result = await _mediator.Send(new BulkCreateAutoReorderRulesCommand(
            request.ReorderThreshold, request.MinIntervalHours, request.AutoForwardToMes));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { createdCount = result.Data });
    }

    [HttpGet("purchase-orders")]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(status, page, pageSize));
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Data);
    }

    [HttpPost("purchase-orders/{id:int}/forward")]
    public async Task<IActionResult> ForwardPurchaseOrder(int id)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po is null) return NotFound(new { message = "발주를 찾을 수 없습니다." });
        if (po.Status != "Created") return BadRequest(new { message = "이미 전송된 발주입니다." });

        var mesItems = po.Items.Where(i => !string.IsNullOrEmpty(i.MesProductCode)).ToList();
        if (mesItems.Count == 0) return BadRequest(new { message = "MES 매핑된 상품이 없습니다." });

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
        await _db.SaveChangesAsync();

        return Ok(new { message = "MES로 전송되었습니다.", mesOrderId = mesResult.MesOrderId });
    }

    [HttpPost("purchase-orders/{id:int}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(int id)
    {
        var result = await _mediator.Send(new CancelPurchaseOrderCommand(id));
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
