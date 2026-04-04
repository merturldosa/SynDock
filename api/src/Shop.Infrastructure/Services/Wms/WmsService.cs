using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class WmsService : IWmsService
{
    private readonly IShopDbContext _db;

    public WmsService(IShopDbContext db) => _db = db;

    // Zone & Location
    public async Task<List<WarehouseZone>> GetZonesAsync(int tenantId, CancellationToken ct = default)
        => await _db.WarehouseZones.AsNoTracking().Where(z => z.IsActive).OrderBy(z => z.SortOrder).Include(z => z.Locations).ToListAsync(ct);

    public async Task<WarehouseZone> CreateZoneAsync(int tenantId, string name, string code, string type, string? description, string createdBy, CancellationToken ct = default)
    {
        var zone = new WarehouseZone { TenantId = tenantId, Name = name, Code = code, Type = type, Description = description, CreatedBy = createdBy };
        _db.WarehouseZones.Add(zone);
        await _db.SaveChangesAsync(ct);
        return zone;
    }

    public async Task<List<WarehouseLocation>> GetLocationsAsync(int tenantId, int? zoneId = null, CancellationToken ct = default)
    {
        var query = _db.WarehouseLocations.AsNoTracking().Include(l => l.Product).AsQueryable();
        if (zoneId.HasValue) query = query.Where(l => l.WarehouseZoneId == zoneId.Value);
        return await query.OrderBy(l => l.Code).ToListAsync(ct);
    }

    public async Task<WarehouseLocation> CreateLocationAsync(int tenantId, int zoneId, string code, string type, int maxCapacity, string createdBy, CancellationToken ct = default)
    {
        var loc = new WarehouseLocation { TenantId = tenantId, WarehouseZoneId = zoneId, Code = code, Type = type, MaxCapacity = maxCapacity, CreatedBy = createdBy };
        _db.WarehouseLocations.Add(loc);
        await _db.SaveChangesAsync(ct);
        return loc;
    }

    public async Task AssignProductToLocationAsync(int tenantId, int locationId, int productId, int quantity, string updatedBy, CancellationToken ct = default)
    {
        var loc = await _db.WarehouseLocations.FirstOrDefaultAsync(l => l.Id == locationId, ct) ?? throw new InvalidOperationException("Location not found");
        loc.ProductId = productId;
        loc.CurrentQuantity = quantity;
        loc.IsOccupied = quantity > 0;
        loc.UpdatedBy = updatedBy;
        loc.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // Inventory Movement
    public async Task<InventoryMovement> RecordMovementAsync(int tenantId, int productId, int? variantId, string movementType, int quantity, int? fromLocationId, int? toLocationId, int? orderId, string? reason, string createdBy, CancellationToken ct = default)
    {
        var variant = variantId.HasValue
            ? await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId.Value, ct)
            : await _db.ProductVariants.FirstOrDefaultAsync(v => v.ProductId == productId, ct);

        var previousStock = variant?.Stock ?? 0;
        var newStock = movementType == "Inbound" || movementType == "Return" ? previousStock + quantity : previousStock - quantity;

        if (variant != null)
        {
            variant.Stock = newStock;
            variant.UpdatedBy = createdBy;
            variant.UpdatedAt = DateTime.UtcNow;
        }

        var movement = new InventoryMovement
        {
            TenantId = tenantId, ProductId = productId, VariantId = variantId,
            MovementType = movementType, Quantity = quantity,
            PreviousStock = previousStock, NewStock = newStock,
            FromLocationId = fromLocationId, ToLocationId = toLocationId,
            OrderId = orderId, Reason = reason, CreatedBy = createdBy
        };
        _db.InventoryMovements.Add(movement);

        // Update location quantities
        if (fromLocationId.HasValue)
        {
            var fromLoc = await _db.WarehouseLocations.FirstOrDefaultAsync(l => l.Id == fromLocationId.Value, ct);
            if (fromLoc != null) { fromLoc.CurrentQuantity = Math.Max(0, fromLoc.CurrentQuantity - quantity); fromLoc.IsOccupied = fromLoc.CurrentQuantity > 0; }
        }
        if (toLocationId.HasValue)
        {
            var toLoc = await _db.WarehouseLocations.FirstOrDefaultAsync(l => l.Id == toLocationId.Value, ct);
            if (toLoc != null) { toLoc.CurrentQuantity += quantity; toLoc.IsOccupied = true; }
        }

        await _db.SaveChangesAsync(ct);
        return movement;
    }

    public async Task<List<InventoryMovement>> GetMovementsAsync(int tenantId, int? productId = null, string? movementType = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.InventoryMovements.AsNoTracking().Include(m => m.Product).AsQueryable();
        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrEmpty(movementType)) query = query.Where(m => m.MovementType == movementType);
        if (from.HasValue) query = query.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(m => m.CreatedAt <= to.Value);
        return await query.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    // Picking
    public async Task<PickingOrder> CreatePickingOrderAsync(int tenantId, int orderId, string createdBy, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new InvalidOperationException("Order not found");
        var pickingNumber = $"PK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var picking = new PickingOrder
        {
            TenantId = tenantId, PickingNumber = pickingNumber, OrderId = orderId,
            TotalItems = order.Items.Sum(i => i.Quantity), CreatedBy = createdBy
        };
        _db.PickingOrders.Add(picking);
        await _db.SaveChangesAsync(ct);

        foreach (var item in order.Items)
        {
            var location = await _db.WarehouseLocations.FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.CurrentQuantity > 0, ct);
            var pickItem = new PickingItem
            {
                TenantId = tenantId, PickingOrderId = picking.Id, ProductId = item.ProductId,
                VariantId = item.VariantId, LocationId = location?.Id ?? 0,
                RequestedQuantity = item.Quantity, CreatedBy = createdBy
            };
            _db.PickingItems.Add(pickItem);
        }
        await _db.SaveChangesAsync(ct);
        return picking;
    }

    public async Task<PickingOrder?> GetPickingOrderAsync(int tenantId, int pickingOrderId, CancellationToken ct = default)
        => await _db.PickingOrders.AsNoTracking().Include(p => p.Items).ThenInclude(i => i.Product).Include(p => p.AssignedUser).FirstOrDefaultAsync(p => p.Id == pickingOrderId, ct);

    public async Task<List<PickingOrder>> GetPickingOrdersAsync(int tenantId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.PickingOrders.AsNoTracking().Include(p => p.AssignedUser).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
        return await query.OrderByDescending(p => p.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task StartPickingAsync(int tenantId, int pickingOrderId, int userId, string updatedBy, CancellationToken ct = default)
    {
        var picking = await _db.PickingOrders.FirstOrDefaultAsync(p => p.Id == pickingOrderId, ct) ?? throw new InvalidOperationException("Picking order not found");
        picking.Status = "InProgress";
        picking.AssignedUserId = userId;
        picking.StartedAt = DateTime.UtcNow;
        picking.UpdatedBy = updatedBy;
        picking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ConfirmPickItemAsync(int tenantId, int pickingItemId, int pickedQuantity, string? barcodeScanned, string updatedBy, CancellationToken ct = default)
    {
        var item = await _db.PickingItems.FirstOrDefaultAsync(i => i.Id == pickingItemId, ct) ?? throw new InvalidOperationException("Picking item not found");
        item.PickedQuantity = pickedQuantity;
        item.Status = pickedQuantity >= item.RequestedQuantity ? "Picked" : "ShortPick";
        item.PickedAt = DateTime.UtcNow;
        item.BarcodeScanned = barcodeScanned;
        item.UpdatedBy = updatedBy;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CompletePickingAsync(int tenantId, int pickingOrderId, string updatedBy, CancellationToken ct = default)
    {
        var picking = await _db.PickingOrders.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == pickingOrderId, ct) ?? throw new InvalidOperationException("Picking order not found");
        picking.Status = "Completed";
        picking.CompletedAt = DateTime.UtcNow;
        picking.PickedItems = picking.Items.Sum(i => i.PickedQuantity);
        picking.UpdatedBy = updatedBy;
        picking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // Packing
    public async Task<PackingSlip> CreatePackingSlipAsync(int tenantId, int orderId, int? pickingOrderId, string createdBy, CancellationToken ct = default)
    {
        var packingNumber = $"PA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var slip = new PackingSlip
        {
            TenantId = tenantId, PackingNumber = packingNumber, OrderId = orderId,
            PickingOrderId = pickingOrderId, CreatedBy = createdBy
        };
        _db.PackingSlips.Add(slip);
        await _db.SaveChangesAsync(ct);
        return slip;
    }

    public async Task<PackingSlip?> GetPackingSlipAsync(int tenantId, int packingSlipId, CancellationToken ct = default)
        => await _db.PackingSlips.AsNoTracking().Include(p => p.Order).Include(p => p.PackedByUser).FirstOrDefaultAsync(p => p.Id == packingSlipId, ct);

    public async Task CompletePackingAsync(int tenantId, int packingSlipId, string? trackingNumber, string? carrierName, decimal totalWeight, string? boxSize, string updatedBy, CancellationToken ct = default)
    {
        var slip = await _db.PackingSlips.FirstOrDefaultAsync(p => p.Id == packingSlipId, ct) ?? throw new InvalidOperationException("Packing slip not found");
        slip.Status = "Packed";
        slip.TrackingNumber = trackingNumber;
        slip.CarrierName = carrierName;
        slip.TotalWeight = totalWeight;
        slip.BoxSize = boxSize;
        slip.PackedAt = DateTime.UtcNow;
        slip.UpdatedBy = updatedBy;
        slip.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkShippedAsync(int tenantId, int packingSlipId, string updatedBy, CancellationToken ct = default)
    {
        var slip = await _db.PackingSlips.FirstOrDefaultAsync(p => p.Id == packingSlipId, ct) ?? throw new InvalidOperationException("Packing slip not found");
        slip.Status = "Shipped";
        slip.ShippedAt = DateTime.UtcNow;
        slip.UpdatedBy = updatedBy;
        slip.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // Barcode
    public async Task<BarcodeMapping> RegisterBarcodeAsync(int tenantId, string barcode, string barcodeType, string entityType, int entityId, string createdBy, CancellationToken ct = default)
    {
        var mapping = new BarcodeMapping { TenantId = tenantId, Barcode = barcode, BarcodeType = barcodeType, EntityType = entityType, EntityId = entityId, CreatedBy = createdBy };
        _db.BarcodeMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return mapping;
    }

    public async Task<BarcodeMapping?> LookupBarcodeAsync(int tenantId, string barcode, CancellationToken ct = default)
        => await _db.BarcodeMappings.AsNoTracking().FirstOrDefaultAsync(b => b.Barcode == barcode && b.IsActive, ct);

    // === Lot Tracking ===

    public async Task<LotTracking> CreateLotAsync(int tenantId, int productId, string lotNumber, string? batchNumber, DateTime? manufacturedDate, DateTime? expiryDate, int quantity, int? locationId, string createdBy, CancellationToken ct = default)
    {
        var lot = new LotTracking
        {
            TenantId = tenantId, ProductId = productId, LotNumber = lotNumber,
            BatchNumber = batchNumber, ManufacturedDate = manufacturedDate,
            ExpiryDate = expiryDate, Quantity = quantity, LocationId = locationId,
            Status = "Available", CreatedBy = createdBy
        };
        _db.LotTrackings.Add(lot);
        await _db.SaveChangesAsync(ct);
        return lot;
    }

    public async Task<List<LotTracking>> GetLotsAsync(int tenantId, int? productId = null, string? status = null, CancellationToken ct = default)
    {
        var query = _db.LotTrackings.AsNoTracking().Include(l => l.Product).Include(l => l.Location).AsQueryable();
        if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    public async Task<List<LotTracking>> GetExpiringLotsAsync(int tenantId, int daysAhead = 30, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(daysAhead);
        return await _db.LotTrackings.AsNoTracking()
            .Include(l => l.Product).Include(l => l.Location)
            .Where(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value <= cutoff && l.Status == "Available")
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(ct);
    }

    // === Goods Receipt (Inbound) ===

    public async Task<GoodsReceipt> CreateGoodsReceiptAsync(int tenantId, int? purchaseOrderId, string? supplierName, List<(int productId, int expectedQty, string? lotNumber, DateTime? expiryDate)> items, string createdBy, CancellationToken ct = default)
    {
        var receiptNumber = $"GR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var totalExpected = items.Sum(i => i.expectedQty);

        var receipt = new GoodsReceipt
        {
            TenantId = tenantId, ReceiptNumber = receiptNumber,
            PurchaseOrderId = purchaseOrderId, SupplierName = supplierName,
            Status = "Pending", ExpectedQuantity = totalExpected,
            CreatedBy = createdBy
        };
        _db.GoodsReceipts.Add(receipt);
        await _db.SaveChangesAsync(ct);

        foreach (var (productId, expectedQty, lotNumber, expiryDate) in items)
        {
            var item = new GoodsReceiptItem
            {
                TenantId = tenantId, GoodsReceiptId = receipt.Id,
                ProductId = productId, ExpectedQuantity = expectedQty,
                LotNumber = lotNumber, ExpiryDate = expiryDate,
                QualityStatus = "Pending", CreatedBy = createdBy
            };
            _db.GoodsReceiptItems.Add(item);
        }
        await _db.SaveChangesAsync(ct);
        return receipt;
    }

    public async Task<GoodsReceipt?> GetGoodsReceiptAsync(int tenantId, int receiptId, CancellationToken ct = default)
        => await _db.GoodsReceipts.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Product)
            .Include(r => r.TargetLocation)
            .Include(r => r.PurchaseOrder)
            .FirstOrDefaultAsync(r => r.Id == receiptId, ct);

    public async Task<List<GoodsReceipt>> GetGoodsReceiptsAsync(int tenantId, string? status = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.GoodsReceipts.AsNoTracking().Include(r => r.Items).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task InspectGoodsReceiptItemAsync(int tenantId, int itemId, int acceptedQty, int rejectedQty, string qualityStatus, string? notes, int inspectorUserId, string updatedBy, CancellationToken ct = default)
    {
        var item = await _db.GoodsReceiptItems.FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new InvalidOperationException("Goods receipt item not found");

        item.ReceivedQuantity = acceptedQty + rejectedQty;
        item.AcceptedQuantity = acceptedQty;
        item.RejectedQuantity = rejectedQty;
        item.QualityStatus = qualityStatus;
        item.Notes = notes;
        item.UpdatedBy = updatedBy;
        item.UpdatedAt = DateTime.UtcNow;

        // Update parent receipt status
        var receipt = await _db.GoodsReceipts.FirstOrDefaultAsync(r => r.Id == item.GoodsReceiptId, ct);
        if (receipt != null)
        {
            receipt.Status = "Inspecting";
            receipt.InspectedByUserId = inspectorUserId;
            receipt.InspectedAt = DateTime.UtcNow;
            receipt.UpdatedBy = updatedBy;
            receipt.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteGoodsReceiptAsync(int tenantId, int receiptId, int? targetLocationId, string updatedBy, CancellationToken ct = default)
    {
        var receipt = await _db.GoodsReceipts.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == receiptId, ct)
            ?? throw new InvalidOperationException("Goods receipt not found");

        receipt.ReceivedQuantity = receipt.Items.Sum(i => i.ReceivedQuantity);
        receipt.AcceptedQuantity = receipt.Items.Sum(i => i.AcceptedQuantity);
        receipt.RejectedQuantity = receipt.Items.Sum(i => i.RejectedQuantity);
        receipt.TargetLocationId = targetLocationId;
        receipt.Status = receipt.RejectedQuantity == 0 ? "Accepted"
            : receipt.AcceptedQuantity == 0 ? "Rejected" : "PartialAccept";
        receipt.UpdatedBy = updatedBy;
        receipt.UpdatedAt = DateTime.UtcNow;

        // Create inventory movements for accepted items and optional lot tracking
        foreach (var item in receipt.Items.Where(i => i.AcceptedQuantity > 0))
        {
            await RecordMovementAsync(tenantId, item.ProductId, null, "Inbound", item.AcceptedQuantity,
                null, targetLocationId, null, $"Goods Receipt {receipt.ReceiptNumber}", updatedBy, ct);

            // Create lot tracking if lot number specified
            if (!string.IsNullOrEmpty(item.LotNumber))
            {
                await CreateLotAsync(tenantId, item.ProductId, item.LotNumber, null,
                    DateTime.UtcNow, item.ExpiryDate, item.AcceptedQuantity, targetLocationId, updatedBy, ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // === Cycle Count ===

    public async Task<CycleCount> CreateCycleCountAsync(int tenantId, int? zoneId, string countType, string createdBy, CancellationToken ct = default)
    {
        var countNumber = $"CC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var cycleCount = new CycleCount
        {
            TenantId = tenantId, CountNumber = countNumber, ZoneId = zoneId,
            Status = "Planned", CountType = countType, CreatedBy = createdBy
        };
        _db.CycleCounts.Add(cycleCount);
        await _db.SaveChangesAsync(ct);

        // Populate items from warehouse locations
        var locationsQuery = _db.WarehouseLocations.Where(l => l.IsOccupied && l.ProductId.HasValue);
        if (zoneId.HasValue) locationsQuery = locationsQuery.Where(l => l.WarehouseZoneId == zoneId.Value);

        var locations = await locationsQuery.ToListAsync(ct);

        // For ABC count types, filter by product revenue ranking
        if (countType.StartsWith("ABC_"))
        {
            var abcData = await GetAbcRankingsAsync(tenantId, ct);
            var targetClass = countType.Replace("ABC_", "");
            var targetProductIds = abcData.Where(a => a.AbcClass == targetClass).Select(a => a.ProductId).ToHashSet();
            locations = locations.Where(l => l.ProductId.HasValue && targetProductIds.Contains(l.ProductId.Value)).ToList();
        }

        foreach (var loc in locations)
        {
            var item = new CycleCountItem
            {
                TenantId = tenantId, CycleCountId = cycleCount.Id,
                ProductId = loc.ProductId!.Value, LocationId = loc.Id,
                SystemQuantity = loc.CurrentQuantity, Status = "Pending",
                CreatedBy = createdBy
            };
            _db.CycleCountItems.Add(item);
        }

        cycleCount.TotalItems = locations.Count;
        cycleCount.StartedAt = DateTime.UtcNow;
        cycleCount.Status = "InProgress";
        await _db.SaveChangesAsync(ct);
        return cycleCount;
    }

    public async Task<CycleCount?> GetCycleCountAsync(int tenantId, int cycleCountId, CancellationToken ct = default)
        => await _db.CycleCounts.AsNoTracking()
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .Include(c => c.Items).ThenInclude(i => i.Location)
            .Include(c => c.Zone)
            .FirstOrDefaultAsync(c => c.Id == cycleCountId, ct);

    public async Task<List<CycleCount>> GetCycleCountsAsync(int tenantId, string? status = null, CancellationToken ct = default)
    {
        var query = _db.CycleCounts.AsNoTracking().Include(c => c.Zone).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task RecordCountAsync(int tenantId, int cycleCountItemId, int countedQuantity, string? notes, string updatedBy, CancellationToken ct = default)
    {
        var item = await _db.CycleCountItems.FirstOrDefaultAsync(i => i.Id == cycleCountItemId, ct)
            ?? throw new InvalidOperationException("Cycle count item not found");

        item.CountedQuantity = countedQuantity;
        item.Discrepancy = countedQuantity - item.SystemQuantity;
        item.Status = "Counted";
        item.CountedAt = DateTime.UtcNow;
        item.Notes = notes;
        item.UpdatedBy = updatedBy;
        item.UpdatedAt = DateTime.UtcNow;

        // Update parent counted items count
        var cycleCount = await _db.CycleCounts.FirstOrDefaultAsync(c => c.Id == item.CycleCountId, ct);
        if (cycleCount != null)
        {
            cycleCount.CountedItems = await _db.CycleCountItems
                .CountAsync(i => i.CycleCountId == cycleCount.Id && i.Status != "Pending", ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteCycleCountAsync(int tenantId, int cycleCountId, string updatedBy, CancellationToken ct = default)
    {
        var cycleCount = await _db.CycleCounts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cycleCountId, ct)
            ?? throw new InvalidOperationException("Cycle count not found");

        cycleCount.Status = "Completed";
        cycleCount.CompletedAt = DateTime.UtcNow;
        cycleCount.CountedItems = cycleCount.Items.Count(i => i.Status != "Pending");
        cycleCount.DiscrepancyItems = cycleCount.Items.Count(i => i.Discrepancy != 0);
        cycleCount.AccuracyPercent = cycleCount.TotalItems > 0
            ? Math.Round((decimal)(cycleCount.TotalItems - cycleCount.DiscrepancyItems) / cycleCount.TotalItems * 100, 2)
            : 100m;
        cycleCount.UpdatedBy = updatedBy;
        cycleCount.UpdatedAt = DateTime.UtcNow;

        // Create adjustment inventory movements for discrepancies
        foreach (var item in cycleCount.Items.Where(i => i.Discrepancy != 0 && i.CountedQuantity.HasValue))
        {
            var movementType = item.Discrepancy > 0 ? "Adjustment_In" : "Adjustment_Out";
            var qty = Math.Abs(item.Discrepancy);
            await RecordMovementAsync(tenantId, item.ProductId, null, movementType, qty,
                item.Discrepancy < 0 ? item.LocationId : null,
                item.Discrepancy > 0 ? item.LocationId : null,
                null, $"Cycle Count {cycleCount.CountNumber} adjustment", updatedBy, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    // === ABC Analysis ===

    public async Task<object> GetAbcAnalysisAsync(int tenantId, CancellationToken ct = default)
    {
        var rankings = await GetAbcRankingsAsync(tenantId, ct);
        var classA = rankings.Where(r => r.AbcClass == "A").ToList();
        var classB = rankings.Where(r => r.AbcClass == "B").ToList();
        var classC = rankings.Where(r => r.AbcClass == "C").ToList();

        return new
        {
            summary = new
            {
                totalProducts = rankings.Count,
                classA = new { count = classA.Count, revenue = classA.Sum(r => r.Revenue), percentOfRevenue = rankings.Count > 0 ? Math.Round(classA.Sum(r => r.Revenue) / Math.Max(1, rankings.Sum(r => r.Revenue)) * 100, 1) : 0 },
                classB = new { count = classB.Count, revenue = classB.Sum(r => r.Revenue), percentOfRevenue = rankings.Count > 0 ? Math.Round(classB.Sum(r => r.Revenue) / Math.Max(1, rankings.Sum(r => r.Revenue)) * 100, 1) : 0 },
                classC = new { count = classC.Count, revenue = classC.Sum(r => r.Revenue), percentOfRevenue = rankings.Count > 0 ? Math.Round(classC.Sum(r => r.Revenue) / Math.Max(1, rankings.Sum(r => r.Revenue)) * 100, 1) : 0 }
            },
            products = rankings
        };
    }

    // === Stock Summary ===

    public async Task<object> GetStockSummaryAsync(int tenantId, CancellationToken ct = default)
    {
        var variants = await _db.ProductVariants.AsNoTracking().ToListAsync(ct);
        var products = await _db.Products.AsNoTracking().ToListAsync(ct);
        var lots = await _db.LotTrackings.AsNoTracking().ToListAsync(ct);

        var totalSKUs = variants.Count;
        var totalQuantity = variants.Sum(v => v.Stock);
        var totalValue = variants.Sum(v => v.Stock * v.Price);
        var lowStockCount = variants.Count(v => v.Stock > 0 && v.Stock <= 10);
        var outOfStockCount = variants.Count(v => v.Stock <= 0);
        var expiredLots = lots.Count(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value <= DateTime.UtcNow && l.Status == "Available");
        var expiringLots = lots.Count(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value > DateTime.UtcNow && l.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30) && l.Status == "Available");

        return new
        {
            totalSKUs,
            totalQuantity,
            totalValue,
            lowStockCount,
            outOfStockCount,
            expiredLots,
            expiringLots,
            totalProducts = products.Count,
            locationUtilization = await GetLocationUtilizationAsync(ct)
        };
    }

    // === Private Helpers ===

    private async Task<List<AbcRanking>> GetAbcRankingsAsync(int tenantId, CancellationToken ct)
    {
        var orderItems = await _db.OrderItems.AsNoTracking()
            .Include(oi => oi.Product)
            .ToListAsync(ct);

        var productRevenues = orderItems
            .GroupBy(oi => new { oi.ProductId, ProductName = oi.Product?.Name ?? "Unknown" })
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity) })
            .OrderByDescending(p => p.Revenue)
            .ToList();

        var totalRevenue = productRevenues.Sum(p => p.Revenue);
        var cumulativeRevenue = 0m;
        var rankings = new List<AbcRanking>();

        foreach (var p in productRevenues)
        {
            cumulativeRevenue += p.Revenue;
            var cumulativePercent = totalRevenue > 0 ? cumulativeRevenue / totalRevenue * 100 : 0;
            var abcClass = cumulativePercent <= 80 ? "A" : cumulativePercent <= 95 ? "B" : "C";
            rankings.Add(new AbcRanking(p.ProductId, p.ProductName, p.Revenue, abcClass));
        }

        return rankings;
    }

    private async Task<object> GetLocationUtilizationAsync(CancellationToken ct)
    {
        var locations = await _db.WarehouseLocations.AsNoTracking().ToListAsync(ct);
        var total = locations.Count;
        var occupied = locations.Count(l => l.IsOccupied);
        return new
        {
            totalLocations = total,
            occupiedLocations = occupied,
            emptyLocations = total - occupied,
            utilizationPercent = total > 0 ? Math.Round((decimal)occupied / total * 100, 1) : 0
        };
    }

    private record AbcRanking(int ProductId, string ProductName, decimal Revenue, string AbcClass);
}
