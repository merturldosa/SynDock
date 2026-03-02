using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shop.Application.Common.Interfaces;
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

    public MesIntegrationController(
        IMesClient mesClient,
        IMesProductMapper mapper,
        IShopDbContext db,
        IConfiguration configuration)
    {
        _mesClient = mesClient;
        _mapper = mapper;
        _db = db;
        _configuration = configuration;
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

        return result.Success
            ? Ok(new { message = "주문이 MES로 전달되었습니다.", mesOrderId = result.MesOrderId })
            : BadRequest(new { message = result.ErrorMessage });
    }
}
