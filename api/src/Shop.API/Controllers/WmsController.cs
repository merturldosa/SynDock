using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class WmsController : ControllerBase
{
    private readonly IWmsService _wms;
    private readonly ICurrentUserService _currentUser;

    public WmsController(IWmsService wms, ICurrentUserService currentUser)
    {
        _wms = wms;
        _currentUser = currentUser;
    }

    // === Zone & Location ===

    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(CancellationToken ct)
        => Ok(await _wms.GetZonesAsync(0, ct));

    [HttpPost("zones")]
    public async Task<IActionResult> CreateZone([FromBody] CreateWarehouseZoneRequest req, CancellationToken ct)
        => Ok(await _wms.CreateZoneAsync(0, req.Name, req.Code, req.Type, req.Description, _currentUser.Username ?? "system", ct));

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations([FromQuery] int? zoneId, CancellationToken ct)
        => Ok(await _wms.GetLocationsAsync(0, zoneId, ct));

    [HttpPost("locations")]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest req, CancellationToken ct)
        => Ok(await _wms.CreateLocationAsync(0, req.ZoneId, req.Code, req.Type, req.MaxCapacity, _currentUser.Username ?? "system", ct));

    [HttpPut("locations/{locationId}/assign")]
    public async Task<IActionResult> AssignProduct(int locationId, [FromBody] AssignProductRequest req, CancellationToken ct)
    {
        await _wms.AssignProductToLocationAsync(0, locationId, req.ProductId, req.Quantity, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Product assigned to location" });
    }

    // === Inventory Movement ===

    [HttpPost("movements")]
    public async Task<IActionResult> RecordMovement([FromBody] RecordMovementRequest req, CancellationToken ct)
        => Ok(await _wms.RecordMovementAsync(0, req.ProductId, req.VariantId, req.MovementType, req.Quantity, req.FromLocationId, req.ToLocationId, req.OrderId, req.Reason, _currentUser.Username ?? "system", ct));

    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] int? productId, [FromQuery] string? movementType, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _wms.GetMovementsAsync(0, productId, movementType, from, to, page, pageSize, ct));

    // === Picking ===

    [HttpPost("picking")]
    public async Task<IActionResult> CreatePickingOrder([FromBody] CreatePickingRequest req, CancellationToken ct)
        => Ok(await _wms.CreatePickingOrderAsync(0, req.OrderId, _currentUser.Username ?? "system", ct));

    [HttpGet("picking")]
    public async Task<IActionResult> GetPickingOrders([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _wms.GetPickingOrdersAsync(0, status, page, pageSize, ct));

    [HttpGet("picking/{id}")]
    public async Task<IActionResult> GetPickingOrder(int id, CancellationToken ct)
    {
        var result = await _wms.GetPickingOrderAsync(0, id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("picking/{id}/start")]
    public async Task<IActionResult> StartPicking(int id, CancellationToken ct)
    {
        await _wms.StartPickingAsync(0, id, _currentUser.UserId ?? 0, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Picking started" });
    }

    [HttpPut("picking/items/{itemId}/confirm")]
    public async Task<IActionResult> ConfirmPickItem(int itemId, [FromBody] ConfirmPickRequest req, CancellationToken ct)
    {
        await _wms.ConfirmPickItemAsync(0, itemId, req.PickedQuantity, req.BarcodeScanned, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Pick confirmed" });
    }

    [HttpPut("picking/{id}/complete")]
    public async Task<IActionResult> CompletePicking(int id, CancellationToken ct)
    {
        await _wms.CompletePickingAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Picking completed" });
    }

    // === Packing ===

    [HttpPost("packing")]
    public async Task<IActionResult> CreatePackingSlip([FromBody] CreatePackingRequest req, CancellationToken ct)
        => Ok(await _wms.CreatePackingSlipAsync(0, req.OrderId, req.PickingOrderId, _currentUser.Username ?? "system", ct));

    [HttpGet("packing/{id}")]
    public async Task<IActionResult> GetPackingSlip(int id, CancellationToken ct)
    {
        var result = await _wms.GetPackingSlipAsync(0, id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("packing/{id}/complete")]
    public async Task<IActionResult> CompletePacking(int id, [FromBody] CompletePackingRequest req, CancellationToken ct)
    {
        await _wms.CompletePackingAsync(0, id, req.TrackingNumber, req.CarrierName, req.TotalWeight, req.BoxSize, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Packing completed" });
    }

    [HttpPut("packing/{id}/ship")]
    public async Task<IActionResult> MarkShipped(int id, CancellationToken ct)
    {
        await _wms.MarkShippedAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Marked as shipped" });
    }

    // === Barcode ===

    [HttpPost("barcodes")]
    public async Task<IActionResult> RegisterBarcode([FromBody] RegisterBarcodeRequest req, CancellationToken ct)
        => Ok(await _wms.RegisterBarcodeAsync(0, req.Barcode, req.BarcodeType, req.EntityType, req.EntityId, _currentUser.Username ?? "system", ct));

    [HttpGet("barcodes/lookup")]
    public async Task<IActionResult> LookupBarcode([FromQuery] string barcode, CancellationToken ct)
    {
        var result = await _wms.LookupBarcodeAsync(0, barcode, ct);
        return result == null ? NotFound() : Ok(result);
    }
    // === Lot Tracking ===

    [HttpPost("lots")]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotRequest req, CancellationToken ct)
        => Ok(await _wms.CreateLotAsync(0, req.ProductId, req.LotNumber, req.BatchNumber, req.ManufacturedDate, req.ExpiryDate, req.Quantity, req.LocationId, _currentUser.Username ?? "system", ct));

    [HttpGet("lots")]
    public async Task<IActionResult> GetLots([FromQuery] int? productId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _wms.GetLotsAsync(0, productId, status, ct));

    [HttpGet("lots/expiring")]
    public async Task<IActionResult> GetExpiringLots([FromQuery] int daysAhead = 30, CancellationToken ct = default)
        => Ok(await _wms.GetExpiringLotsAsync(0, daysAhead, ct));

    // === Goods Receipt (Inbound) ===

    [HttpPost("goods-receipts")]
    public async Task<IActionResult> CreateGoodsReceipt([FromBody] CreateGoodsReceiptRequest req, CancellationToken ct)
    {
        var items = req.Items.Select(i => (i.ProductId, i.ExpectedQuantity, i.LotNumber, i.ExpiryDate)).ToList();
        return Ok(await _wms.CreateGoodsReceiptAsync(0, req.PurchaseOrderId, req.SupplierName, items, _currentUser.Username ?? "system", ct));
    }

    [HttpGet("goods-receipts")]
    public async Task<IActionResult> GetGoodsReceipts([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _wms.GetGoodsReceiptsAsync(0, status, page, pageSize, ct));

    [HttpGet("goods-receipts/{id}")]
    public async Task<IActionResult> GetGoodsReceipt(int id, CancellationToken ct)
    {
        var result = await _wms.GetGoodsReceiptAsync(0, id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("goods-receipts/items/{itemId}/inspect")]
    public async Task<IActionResult> InspectGoodsReceiptItem(int itemId, [FromBody] InspectItemRequest req, CancellationToken ct)
    {
        await _wms.InspectGoodsReceiptItemAsync(0, itemId, req.AcceptedQuantity, req.RejectedQuantity, req.QualityStatus, req.Notes, _currentUser.UserId ?? 0, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Item inspected" });
    }

    [HttpPut("goods-receipts/{id}/complete")]
    public async Task<IActionResult> CompleteGoodsReceipt(int id, [FromBody] CompleteGoodsReceiptRequest req, CancellationToken ct)
    {
        await _wms.CompleteGoodsReceiptAsync(0, id, req.TargetLocationId, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Goods receipt completed" });
    }

    // === Cycle Count ===

    [HttpPost("cycle-counts")]
    public async Task<IActionResult> CreateCycleCount([FromBody] CreateCycleCountRequest req, CancellationToken ct)
        => Ok(await _wms.CreateCycleCountAsync(0, req.ZoneId, req.CountType, _currentUser.Username ?? "system", ct));

    [HttpGet("cycle-counts")]
    public async Task<IActionResult> GetCycleCounts([FromQuery] string? status, CancellationToken ct = default)
        => Ok(await _wms.GetCycleCountsAsync(0, status, ct));

    [HttpGet("cycle-counts/{id}")]
    public async Task<IActionResult> GetCycleCount(int id, CancellationToken ct)
    {
        var result = await _wms.GetCycleCountAsync(0, id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPut("cycle-counts/items/{itemId}/record")]
    public async Task<IActionResult> RecordCount(int itemId, [FromBody] RecordCountRequest req, CancellationToken ct)
    {
        await _wms.RecordCountAsync(0, itemId, req.CountedQuantity, req.Notes, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Count recorded" });
    }

    [HttpPut("cycle-counts/{id}/complete")]
    public async Task<IActionResult> CompleteCycleCount(int id, CancellationToken ct)
    {
        await _wms.CompleteCycleCountAsync(0, id, _currentUser.Username ?? "system", ct);
        return Ok(new { message = "Cycle count completed" });
    }

    // === Analytics ===

    [HttpGet("abc-analysis")]
    public async Task<IActionResult> GetAbcAnalysis(CancellationToken ct)
        => Ok(await _wms.GetAbcAnalysisAsync(0, ct));

    [HttpGet("stock-summary")]
    public async Task<IActionResult> GetStockSummary(CancellationToken ct)
        => Ok(await _wms.GetStockSummaryAsync(0, ct));
}

// Request DTOs
public record CreateLotRequest(int ProductId, string LotNumber, string? BatchNumber = null, DateTime? ManufacturedDate = null, DateTime? ExpiryDate = null, int Quantity = 0, int? LocationId = null);
public record CreateGoodsReceiptRequest(int? PurchaseOrderId, string? SupplierName, List<GoodsReceiptItemRequest> Items);
public record GoodsReceiptItemRequest(int ProductId, int ExpectedQuantity, string? LotNumber = null, DateTime? ExpiryDate = null);
public record InspectItemRequest(int AcceptedQuantity, int RejectedQuantity, string QualityStatus, string? Notes = null);
public record CompleteGoodsReceiptRequest(int? TargetLocationId = null);
public record CreateCycleCountRequest(int? ZoneId = null, string CountType = "Full");
public record RecordCountRequest(int CountedQuantity, string? Notes = null);
public record CreateWarehouseZoneRequest(string Name, string Code, string Type = "General", string? Description = null);
public record CreateLocationRequest(int ZoneId, string Code, string Type = "Shelf", int MaxCapacity = 1000);
public record AssignProductRequest(int ProductId, int Quantity);
public record RecordMovementRequest(int ProductId, int? VariantId, string MovementType, int Quantity, int? FromLocationId, int? ToLocationId, int? OrderId, string? Reason);
public record CreatePickingRequest(int OrderId);
public record ConfirmPickRequest(int PickedQuantity, string? BarcodeScanned);
public record CreatePackingRequest(int OrderId, int? PickingOrderId);
public record CompletePackingRequest(string? TrackingNumber, string? CarrierName, decimal TotalWeight, string? BoxSize);
public record RegisterBarcodeRequest(string Barcode, string BarcodeType, string EntityType, int EntityId);
