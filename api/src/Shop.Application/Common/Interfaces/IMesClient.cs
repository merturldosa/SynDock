namespace Shop.Application.Common.Interfaces;

public record MesInventoryItem(
    string ProductCode,
    string? ProductName,
    decimal AvailableQuantity,
    int ReservedQty,
    string? WarehouseCode,
    string? WarehouseName,
    DateTime? LastUpdated);

public record MesSalesOrderRequest(
    string OrderNo,
    string OrderDate,
    long CustomerId,
    long SalesUserId,
    List<MesSalesOrderLine> Items);

public record MesSalesOrderLine(
    int LineNo,
    long ProductId,
    decimal OrderedQuantity,
    string Unit,
    decimal UnitPrice);

public record MesSalesOrderResult(
    bool Success,
    string? MesOrderId,
    string? ErrorMessage);

public record MesSyncStatus(
    bool IsConnected,
    DateTime? LastSyncAt,
    int SyncedProductCount,
    string? ErrorMessage);

public record MesStockDiscrepancy(
    int ProductId,
    string ProductName,
    string? ProductCode,
    int ShopStock,
    int MesStock,
    int Difference);

public record MesInventoryComparison(
    int? ShopProductId,
    string ProductName,
    string? MesProductCode,
    int ShopStock,
    int MesStock,
    int Difference,
    string Status); // "matched" | "discrepancy" | "shop_only" | "mes_only"

public interface IMesClient
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<List<MesInventoryItem>> GetInventoryAsync(CancellationToken ct = default);
    Task<MesSalesOrderResult> CreateSalesOrderAsync(MesSalesOrderRequest request, CancellationToken ct = default);
    Task<MesSyncStatus> GetSyncStatusAsync(CancellationToken ct = default);
}

public interface IMesProductMapper
{
    Task<string?> GetMesProductCodeAsync(int productId, CancellationToken ct = default);
    Task<int?> GetShopProductIdAsync(string mesProductCode, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetAllMappingsAsync(CancellationToken ct = default);
}
