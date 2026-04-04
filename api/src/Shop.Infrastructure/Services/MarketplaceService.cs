using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly IShopDbContext _db;
    private readonly ILogger<MarketplaceService> _logger;

    private static readonly Dictionary<string, string> MarketplaceNames = new()
    {
        ["Coupang"] = "쿠팡",
        ["Gmarket"] = "지마켓",
        ["NaverSmart"] = "네이버 스마트스토어",
        ["St11"] = "11번가",
        ["SSG"] = "SSG.COM",
        ["Amazon"] = "Amazon",
        ["Shopify"] = "Shopify"
    };

    public MarketplaceService(IShopDbContext db, ILogger<MarketplaceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Connections ───────────────────────────────────────────────

    public async Task<List<MarketplaceConnection>> GetConnectionsAsync(int tenantId, CancellationToken ct = default)
    {
        return await _db.MarketplaceConnections
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.MarketplaceName)
            .ToListAsync(ct);
    }

    public async Task<MarketplaceConnection> ConnectMarketplaceAsync(
        int tenantId, string marketplaceCode, string? apiKey, string? apiSecret,
        string? sellerId, decimal priceMarkupPercent, string createdBy, CancellationToken ct = default)
    {
        if (!MarketplaceNames.ContainsKey(marketplaceCode))
            throw new ArgumentException($"Unsupported marketplace code: {marketplaceCode}");

        var existing = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.MarketplaceCode == marketplaceCode, ct);

        if (existing != null)
        {
            existing.ApiKey = apiKey;
            existing.ApiSecret = apiSecret;
            existing.SellerId = sellerId;
            existing.PriceMarkupPercent = priceMarkupPercent;
            existing.Status = "Connected";
            existing.UpdatedBy = createdBy;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Updated marketplace connection {Code} for tenant {TenantId}", marketplaceCode, tenantId);
            return existing;
        }

        var connection = new MarketplaceConnection
        {
            TenantId = tenantId,
            MarketplaceCode = marketplaceCode,
            MarketplaceName = MarketplaceNames[marketplaceCode],
            ApiKey = apiKey,
            ApiSecret = apiSecret,
            SellerId = sellerId,
            PriceMarkupPercent = priceMarkupPercent,
            Status = "Connected",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.MarketplaceConnections.Add(connection);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Connected marketplace {Code} for tenant {TenantId}", marketplaceCode, tenantId);
        return connection;
    }

    public async Task DisconnectMarketplaceAsync(int tenantId, int connectionId, string updatedBy, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        connection.Status = "Disconnected";
        connection.UpdatedBy = updatedBy;
        connection.UpdatedAt = DateTime.UtcNow;

        // Pause all active listings
        var listings = await _db.MarketplaceListings
            .Where(l => l.MarketplaceConnectionId == connectionId && l.Status == "Listed")
            .ToListAsync(ct);

        foreach (var listing in listings)
        {
            listing.Status = "Paused";
            listing.UpdatedBy = updatedBy;
            listing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Disconnected marketplace connection {Id}, paused {Count} listings", connectionId, listings.Count);
    }

    public async Task<object> TestConnectionAsync(int tenantId, int connectionId, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        // In production: call the actual marketplace API to verify credentials
        // For now: simulate connection test
        var success = !string.IsNullOrEmpty(connection.ApiKey) || !string.IsNullOrEmpty(connection.AccessToken);

        return new
        {
            connectionId = connection.Id,
            marketplaceCode = connection.MarketplaceCode,
            success,
            message = success ? "Connection successful" : "Missing API credentials",
            testedAt = DateTime.UtcNow
        };
    }

    // ── Listings ──────────────────────────────────────────────────

    public async Task<List<MarketplaceListing>> BulkListProductsAsync(
        int tenantId, int connectionId, List<int> productIds, string? externalCategoryId,
        string createdBy, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        if (connection.Status != "Connected")
            throw new InvalidOperationException("Marketplace is not connected");

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.TenantId == tenantId && p.IsActive)
            .ToListAsync(ct);

        var variants = await _db.ProductVariants
            .Where(v => productIds.Contains(v.ProductId) && v.TenantId == tenantId)
            .ToListAsync(ct);

        var variantsByProduct = variants.GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var existingListings = await _db.MarketplaceListings
            .Where(l => l.MarketplaceConnectionId == connectionId && productIds.Contains(l.ProductId))
            .Select(l => l.ProductId)
            .ToListAsync(ct);

        var results = new List<MarketplaceListing>();
        var markupMultiplier = 1 + (connection.PriceMarkupPercent / 100m);

        foreach (var product in products)
        {
            if (existingListings.Contains(product.Id))
                continue; // Already listed

            var stock = variantsByProduct.ContainsKey(product.Id)
                ? variantsByProduct[product.Id].Sum(v => v.Stock)
                : 0;

            var listedPrice = Math.Round(product.Price * markupMultiplier);

            // In production: call marketplace API to register the product
            // For now: simulate successful listing with mock external IDs
            var mockExternalId = $"{connection.MarketplaceCode}-{tenantId}-{product.Id}-{Guid.NewGuid():N[..8]}";

            var listing = new MarketplaceListing
            {
                TenantId = tenantId,
                MarketplaceConnectionId = connectionId,
                ProductId = product.Id,
                MarketplaceCode = connection.MarketplaceCode,
                ExternalProductId = mockExternalId,
                ExternalUrl = $"https://{connection.MarketplaceCode.ToLower()}.example.com/products/{mockExternalId}",
                Status = "Listed",
                ListedPrice = listedPrice,
                ListedStock = stock,
                ExternalCategoryId = externalCategoryId,
                ListedAt = DateTime.UtcNow,
                LastStockSyncAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _db.MarketplaceListings.Add(listing);
            results.Add(listing);
        }

        // Update connection stats
        connection.ProductsSynced += results.Count;
        connection.LastSyncAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Bulk listed {Count} products on {Marketplace} for tenant {TenantId}",
            results.Count, connection.MarketplaceCode, tenantId);

        return results;
    }

    public async Task<List<MarketplaceListing>> GetListingsAsync(
        int tenantId, int? connectionId = null, string? status = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.MarketplaceListings
            .Include(l => l.Product)
            .Where(l => l.TenantId == tenantId);

        if (connectionId.HasValue)
            query = query.Where(l => l.MarketplaceConnectionId == connectionId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        return await query
            .OrderByDescending(l => l.ListedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task DelistProductAsync(int tenantId, int listingId, string updatedBy, CancellationToken ct = default)
    {
        var listing = await _db.MarketplaceListings
            .FirstOrDefaultAsync(l => l.Id == listingId && l.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Listing {listingId} not found");

        // In production: call marketplace API to delist the product
        listing.Status = "Delisted";
        listing.UpdatedBy = updatedBy;
        listing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Delisted product {ProductId} from {Marketplace}", listing.ProductId, listing.MarketplaceCode);
    }

    public async Task SyncStockAsync(int tenantId, int? connectionId = null, CancellationToken ct = default)
    {
        var query = _db.MarketplaceListings
            .Where(l => l.TenantId == tenantId && l.Status == "Listed");

        if (connectionId.HasValue)
            query = query.Where(l => l.MarketplaceConnectionId == connectionId.Value);

        var listings = await query.ToListAsync(ct);
        if (!listings.Any()) return;

        var productIds = listings.Select(l => l.ProductId).Distinct().ToList();
        var variants = await _db.ProductVariants
            .Where(v => productIds.Contains(v.ProductId) && v.TenantId == tenantId)
            .ToListAsync(ct);

        var stockByProduct = variants.GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Stock));

        var updatedCount = 0;
        foreach (var listing in listings)
        {
            var currentStock = stockByProduct.GetValueOrDefault(listing.ProductId, 0);
            if (listing.ListedStock != currentStock)
            {
                listing.ListedStock = currentStock;
                listing.LastStockSyncAt = DateTime.UtcNow;
                listing.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
                // In production: call marketplace API to update stock
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced stock for {Count}/{Total} listings, tenant {TenantId}",
                updatedCount, listings.Count, tenantId);
        }
    }

    public async Task SyncPricesAsync(int tenantId, int? connectionId = null, CancellationToken ct = default)
    {
        var query = _db.MarketplaceListings
            .Include(l => l.Connection)
            .Include(l => l.Product)
            .Where(l => l.TenantId == tenantId && l.Status == "Listed");

        if (connectionId.HasValue)
            query = query.Where(l => l.MarketplaceConnectionId == connectionId.Value);

        var listings = await query.ToListAsync(ct);
        var updatedCount = 0;

        foreach (var listing in listings)
        {
            var markupMultiplier = 1 + (listing.Connection.PriceMarkupPercent / 100m);
            var newPrice = Math.Round(listing.Product.Price * markupMultiplier);

            if (listing.ListedPrice != newPrice)
            {
                listing.ListedPrice = newPrice;
                listing.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
                // In production: call marketplace API to update price
            }
        }

        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Synced prices for {Count} listings, tenant {TenantId}", updatedCount, tenantId);
        }
    }

    // ── Orders ────────────────────────────────────────────────────

    public async Task<List<MarketplaceOrder>> GetExternalOrdersAsync(
        int tenantId, int? connectionId = null, string? status = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.MarketplaceOrders
            .Where(o => o.TenantId == tenantId);

        if (connectionId.HasValue)
            query = query.Where(o => o.MarketplaceConnectionId == connectionId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        return await query
            .OrderByDescending(o => o.OrderedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task SyncOrdersAsync(int tenantId, int connectionId, CancellationToken ct = default)
    {
        var connection = await _db.MarketplaceConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Connection {connectionId} not found");

        // In production: call marketplace API to fetch new orders
        // For now: simulate by logging
        _logger.LogInformation("Order sync triggered for {Marketplace}, tenant {TenantId}. " +
            "In production, this would fetch orders from the marketplace API.",
            connection.MarketplaceCode, tenantId);

        connection.LastSyncAt = DateTime.UtcNow;
        connection.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateShippingAsync(int tenantId, int orderId, string trackingNumber, string updatedBy, CancellationToken ct = default)
    {
        var order = await _db.MarketplaceOrders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException($"Marketplace order {orderId} not found");

        order.TrackingNumber = trackingNumber;
        order.Status = "Shipped";
        order.ShippedAt = DateTime.UtcNow;
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;

        // In production: call marketplace API to update shipping info
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Updated shipping for marketplace order {OrderId}: {TrackingNumber}", orderId, trackingNumber);
    }

    // ── Dashboard ─────────────────────────────────────────────────

    public async Task<object> GetMarketplaceDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var connections = await _db.MarketplaceConnections
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(ct);

        var listings = await _db.MarketplaceListings
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(ct);

        var orders = await _db.MarketplaceOrders
            .Where(o => o.TenantId == tenantId)
            .ToListAsync(ct);

        var perMarketplace = connections.Select(c => new
        {
            connectionId = c.Id,
            marketplaceCode = c.MarketplaceCode,
            marketplaceName = c.MarketplaceName,
            status = c.Status,
            totalListings = listings.Count(l => l.MarketplaceConnectionId == c.Id),
            listedCount = listings.Count(l => l.MarketplaceConnectionId == c.Id && l.Status == "Listed"),
            pausedCount = listings.Count(l => l.MarketplaceConnectionId == c.Id && l.Status == "Paused"),
            failedCount = listings.Count(l => l.MarketplaceConnectionId == c.Id && l.Status == "Failed"),
            totalOrders = orders.Count(o => o.MarketplaceConnectionId == c.Id),
            totalRevenue = orders.Where(o => o.MarketplaceConnectionId == c.Id).Sum(o => o.NetAmount),
            lastSyncAt = c.LastSyncAt
        }).ToList();

        return new
        {
            summary = new
            {
                connectedMarketplaces = connections.Count(c => c.Status == "Connected"),
                totalMarketplaces = connections.Count,
                totalProductsListed = listings.Count(l => l.Status == "Listed"),
                totalExternalOrders = orders.Count,
                totalRevenue = orders.Sum(o => o.NetAmount),
                revenueByMarketplace = perMarketplace
                    .Where(p => p.totalRevenue > 0)
                    .Select(p => new { p.marketplaceCode, p.marketplaceName, p.totalRevenue })
                    .ToList()
            },
            marketplaces = perMarketplace
        };
    }

    public Task<List<object>> GetAvailableMarketplacesAsync(CancellationToken ct = default)
    {
        var marketplaces = new List<object>
        {
            new { code = "Coupang", name = "쿠팡", description = "국내 최대 이커머스", logo = "coupang.svg", apiDocsUrl = "https://developers.coupang.com" },
            new { code = "Gmarket", name = "지마켓", description = "G마켓/옥션 통합", logo = "gmarket.svg", apiDocsUrl = "https://developer.gmarket.co.kr" },
            new { code = "NaverSmart", name = "네이버 스마트스토어", description = "네이버 커머스", logo = "naver.svg", apiDocsUrl = "https://apicenter.commerce.naver.com" },
            new { code = "St11", name = "11번가", description = "SK 이커머스", logo = "11st.svg", apiDocsUrl = "https://openapi.11st.co.kr" },
            new { code = "SSG", name = "SSG.COM", description = "신세계 온라인", logo = "ssg.svg", apiDocsUrl = "https://developers.ssg.com" },
            new { code = "Amazon", name = "Amazon", description = "글로벌 마켓플레이스", logo = "amazon.svg", apiDocsUrl = "https://developer-docs.amazon.com/sp-api" },
            new { code = "Shopify", name = "Shopify", description = "글로벌 커머스 플랫폼", logo = "shopify.svg", apiDocsUrl = "https://shopify.dev/docs/api" }
        };

        return Task.FromResult(marketplaces);
    }
}
