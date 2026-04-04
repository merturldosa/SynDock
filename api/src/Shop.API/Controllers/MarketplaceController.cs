using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,TenantAdmin,PlatformAdmin")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplaceService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<MarketplaceController> _logger;

    public MarketplaceController(
        IMarketplaceService marketplaceService,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        ILogger<MarketplaceController> logger)
    {
        _marketplaceService = marketplaceService;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ── Available Marketplaces ────────────────────────────────────

    /// <summary>Get list of supported marketplaces</summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableMarketplaces(CancellationToken ct)
    {
        var result = await _marketplaceService.GetAvailableMarketplacesAsync(ct);
        return Ok(result);
    }

    // ── Connections ───────────────────────────────────────────────

    /// <summary>Get all marketplace connections for current tenant</summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GetConnections(CancellationToken ct)
    {
        var connections = await _marketplaceService.GetConnectionsAsync(_tenantContext.TenantId, ct);
        return Ok(connections);
    }

    /// <summary>Connect to a marketplace</summary>
    [HttpPost("connections")]
    public async Task<IActionResult> ConnectMarketplace([FromBody] ConnectMarketplaceRequest request, CancellationToken ct)
    {
        try
        {
            var connection = await _marketplaceService.ConnectMarketplaceAsync(
                _tenantContext.TenantId, request.MarketplaceCode, request.ApiKey,
                request.ApiSecret, request.SellerId, request.PriceMarkupPercent,
                _currentUser.Username, ct);
            return Ok(connection);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Disconnect a marketplace</summary>
    [HttpDelete("connections/{connectionId}")]
    public async Task<IActionResult> DisconnectMarketplace(int connectionId, CancellationToken ct)
    {
        try
        {
            await _marketplaceService.DisconnectMarketplaceAsync(_tenantContext.TenantId, connectionId, _currentUser.Username, ct);
            return Ok(new { message = "Marketplace disconnected" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Test marketplace connection</summary>
    [HttpPost("connections/{connectionId}/test")]
    public async Task<IActionResult> TestConnection(int connectionId, CancellationToken ct)
    {
        try
        {
            var result = await _marketplaceService.TestConnectionAsync(_tenantContext.TenantId, connectionId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ── Listings ──────────────────────────────────────────────────

    /// <summary>Get product listings across marketplaces</summary>
    [HttpGet("listings")]
    public async Task<IActionResult> GetListings(
        [FromQuery] int? connectionId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var listings = await _marketplaceService.GetListingsAsync(
            _tenantContext.TenantId, connectionId, status, page, pageSize, ct);
        return Ok(listings);
    }

    /// <summary>Bulk list products on a marketplace</summary>
    [HttpPost("listings/bulk")]
    public async Task<IActionResult> BulkListProducts([FromBody] BulkListRequest request, CancellationToken ct)
    {
        try
        {
            var listings = await _marketplaceService.BulkListProductsAsync(
                _tenantContext.TenantId, request.ConnectionId, request.ProductIds,
                request.ExternalCategoryId, _currentUser.Username, ct);
            return Ok(new { listed = listings.Count, listings });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delist a product from marketplace</summary>
    [HttpDelete("listings/{listingId}")]
    public async Task<IActionResult> DelistProduct(int listingId, CancellationToken ct)
    {
        try
        {
            await _marketplaceService.DelistProductAsync(_tenantContext.TenantId, listingId, _currentUser.Username, ct);
            return Ok(new { message = "Product delisted" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Sync stock levels to marketplaces</summary>
    [HttpPost("sync/stock")]
    public async Task<IActionResult> SyncStock([FromQuery] int? connectionId, CancellationToken ct)
    {
        await _marketplaceService.SyncStockAsync(_tenantContext.TenantId, connectionId, ct);
        return Ok(new { message = "Stock sync completed" });
    }

    /// <summary>Sync prices to marketplaces</summary>
    [HttpPost("sync/prices")]
    public async Task<IActionResult> SyncPrices([FromQuery] int? connectionId, CancellationToken ct)
    {
        await _marketplaceService.SyncPricesAsync(_tenantContext.TenantId, connectionId, ct);
        return Ok(new { message = "Price sync completed" });
    }

    // ── Orders ────────────────────────────────────────────────────

    /// <summary>Get external marketplace orders</summary>
    [HttpGet("orders")]
    public async Task<IActionResult> GetExternalOrders(
        [FromQuery] int? connectionId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var orders = await _marketplaceService.GetExternalOrdersAsync(
            _tenantContext.TenantId, connectionId, status, page, pageSize, ct);
        return Ok(orders);
    }

    /// <summary>Sync orders from a marketplace</summary>
    [HttpPost("orders/sync/{connectionId}")]
    public async Task<IActionResult> SyncOrders(int connectionId, CancellationToken ct)
    {
        try
        {
            await _marketplaceService.SyncOrdersAsync(_tenantContext.TenantId, connectionId, ct);
            return Ok(new { message = "Order sync triggered" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Update shipping info for marketplace order</summary>
    [HttpPut("orders/{orderId}/shipping")]
    public async Task<IActionResult> UpdateShipping(int orderId, [FromBody] MarketplaceUpdateShippingRequest request, CancellationToken ct)
    {
        try
        {
            await _marketplaceService.UpdateShippingAsync(
                _tenantContext.TenantId, orderId, request.TrackingNumber, _currentUser.Username, ct);
            return Ok(new { message = "Shipping updated" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ── Dashboard ─────────────────────────────────────────────────

    /// <summary>Get marketplace integration dashboard</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _marketplaceService.GetMarketplaceDashboardAsync(_tenantContext.TenantId, ct);
        return Ok(dashboard);
    }
}

// ── Request DTOs ──────────────────────────────────────────────────

public record ConnectMarketplaceRequest(
    string MarketplaceCode,
    string? ApiKey,
    string? ApiSecret,
    string? SellerId,
    decimal PriceMarkupPercent = 0);

public record BulkListRequest(
    int ConnectionId,
    List<int> ProductIds,
    string? ExternalCategoryId);

public record MarketplaceUpdateShippingRequest(string TrackingNumber);
