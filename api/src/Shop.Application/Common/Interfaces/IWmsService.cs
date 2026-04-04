using Shop.Domain.Entities;

namespace Shop.Application.Common.Interfaces;

public interface IWmsService
{
    // Zone & Location
    Task<List<WarehouseZone>> GetZonesAsync(int tenantId, CancellationToken ct = default);
    Task<WarehouseZone> CreateZoneAsync(int tenantId, string name, string code, string type, string? description, string createdBy, CancellationToken ct = default);
    Task<List<WarehouseLocation>> GetLocationsAsync(int tenantId, int? zoneId = null, CancellationToken ct = default);
    Task<WarehouseLocation> CreateLocationAsync(int tenantId, int zoneId, string code, string type, int maxCapacity, string createdBy, CancellationToken ct = default);
    Task AssignProductToLocationAsync(int tenantId, int locationId, int productId, int quantity, string updatedBy, CancellationToken ct = default);

    // Inventory Movement
    Task<InventoryMovement> RecordMovementAsync(int tenantId, int productId, int? variantId, string movementType, int quantity, int? fromLocationId, int? toLocationId, int? orderId, string? reason, string createdBy, CancellationToken ct = default);
    Task<List<InventoryMovement>> GetMovementsAsync(int tenantId, int? productId = null, string? movementType = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default);

    // Picking
    Task<PickingOrder> CreatePickingOrderAsync(int tenantId, int orderId, string createdBy, CancellationToken ct = default);
    Task<PickingOrder?> GetPickingOrderAsync(int tenantId, int pickingOrderId, CancellationToken ct = default);
    Task<List<PickingOrder>> GetPickingOrdersAsync(int tenantId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task StartPickingAsync(int tenantId, int pickingOrderId, int userId, string updatedBy, CancellationToken ct = default);
    Task ConfirmPickItemAsync(int tenantId, int pickingItemId, int pickedQuantity, string? barcodeScanned, string updatedBy, CancellationToken ct = default);
    Task CompletePickingAsync(int tenantId, int pickingOrderId, string updatedBy, CancellationToken ct = default);

    // Packing
    Task<PackingSlip> CreatePackingSlipAsync(int tenantId, int orderId, int? pickingOrderId, string createdBy, CancellationToken ct = default);
    Task<PackingSlip?> GetPackingSlipAsync(int tenantId, int packingSlipId, CancellationToken ct = default);
    Task CompletePackingAsync(int tenantId, int packingSlipId, string? trackingNumber, string? carrierName, decimal totalWeight, string? boxSize, string updatedBy, CancellationToken ct = default);
    Task MarkShippedAsync(int tenantId, int packingSlipId, string updatedBy, CancellationToken ct = default);

    // Barcode
    Task<BarcodeMapping> RegisterBarcodeAsync(int tenantId, string barcode, string barcodeType, string entityType, int entityId, string createdBy, CancellationToken ct = default);
    Task<BarcodeMapping?> LookupBarcodeAsync(int tenantId, string barcode, CancellationToken ct = default);

    // Lot Tracking
    Task<LotTracking> CreateLotAsync(int tenantId, int productId, string lotNumber, string? batchNumber, DateTime? manufacturedDate, DateTime? expiryDate, int quantity, int? locationId, string createdBy, CancellationToken ct = default);
    Task<List<LotTracking>> GetLotsAsync(int tenantId, int? productId = null, string? status = null, CancellationToken ct = default);
    Task<List<LotTracking>> GetExpiringLotsAsync(int tenantId, int daysAhead = 30, CancellationToken ct = default);

    // Goods Receipt (Inbound)
    Task<GoodsReceipt> CreateGoodsReceiptAsync(int tenantId, int? purchaseOrderId, string? supplierName, List<(int productId, int expectedQty, string? lotNumber, DateTime? expiryDate)> items, string createdBy, CancellationToken ct = default);
    Task<GoodsReceipt?> GetGoodsReceiptAsync(int tenantId, int receiptId, CancellationToken ct = default);
    Task<List<GoodsReceipt>> GetGoodsReceiptsAsync(int tenantId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task InspectGoodsReceiptItemAsync(int tenantId, int itemId, int acceptedQty, int rejectedQty, string qualityStatus, string? notes, int inspectorUserId, string updatedBy, CancellationToken ct = default);
    Task CompleteGoodsReceiptAsync(int tenantId, int receiptId, int? targetLocationId, string updatedBy, CancellationToken ct = default);

    // Cycle Count
    Task<CycleCount> CreateCycleCountAsync(int tenantId, int? zoneId, string countType, string createdBy, CancellationToken ct = default);
    Task<CycleCount?> GetCycleCountAsync(int tenantId, int cycleCountId, CancellationToken ct = default);
    Task<List<CycleCount>> GetCycleCountsAsync(int tenantId, string? status = null, CancellationToken ct = default);
    Task RecordCountAsync(int tenantId, int cycleCountItemId, int countedQuantity, string? notes, string updatedBy, CancellationToken ct = default);
    Task CompleteCycleCountAsync(int tenantId, int cycleCountId, string updatedBy, CancellationToken ct = default);

    // ABC Analysis
    Task<object> GetAbcAnalysisAsync(int tenantId, CancellationToken ct = default);

    // Stock Summary
    Task<object> GetStockSummaryAsync(int tenantId, CancellationToken ct = default);
}
