using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IMarketplaceService
{
    // Connections
    Task<List<MarketplaceConnection>> GetConnectionsAsync(int tenantId, CancellationToken ct = default);
    Task<MarketplaceConnection> ConnectMarketplaceAsync(int tenantId, string marketplaceCode, string? apiKey, string? apiSecret, string? sellerId, decimal priceMarkupPercent, string createdBy, CancellationToken ct = default);
    Task DisconnectMarketplaceAsync(int tenantId, int connectionId, string updatedBy, CancellationToken ct = default);
    Task<object> TestConnectionAsync(int tenantId, int connectionId, CancellationToken ct = default);

    // Listings
    Task<List<MarketplaceListing>> BulkListProductsAsync(int tenantId, int connectionId, List<int> productIds, string? externalCategoryId, string createdBy, CancellationToken ct = default);
    Task<List<MarketplaceListing>> GetListingsAsync(int tenantId, int? connectionId = null, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task DelistProductAsync(int tenantId, int listingId, string updatedBy, CancellationToken ct = default);
    Task SyncStockAsync(int tenantId, int? connectionId = null, CancellationToken ct = default);
    Task SyncPricesAsync(int tenantId, int? connectionId = null, CancellationToken ct = default);

    // Orders
    Task<List<MarketplaceOrder>> GetExternalOrdersAsync(int tenantId, int? connectionId = null, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task SyncOrdersAsync(int tenantId, int connectionId, CancellationToken ct = default);
    Task UpdateShippingAsync(int tenantId, int orderId, string trackingNumber, string updatedBy, CancellationToken ct = default);

    // Dashboard
    Task<object> GetMarketplaceDashboardAsync(int tenantId, CancellationToken ct = default);
    Task<List<object>> GetAvailableMarketplacesAsync(CancellationToken ct = default);
}
