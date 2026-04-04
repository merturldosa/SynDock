using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Integration;

/// <summary>
/// Demo MES client that returns realistic mock data for testing/demo purposes.
/// Used when Mes:Enabled is "demo" in configuration.
/// </summary>
public class DemoMesClient : IMesClient
{
    private static readonly List<MesInventoryItem> _inventory = new()
    {
        new("CTH-ROS-001", "5단 묵주 (자수정)", 150, 10, "WH-01", "본사 창고", DateTime.UtcNow.AddMinutes(-30)),
        new("CTH-ROS-002", "5단 묵주 (장미)", 85, 5, "WH-01", "본사 창고", DateTime.UtcNow.AddMinutes(-30)),
        new("CTH-ROS-003", "원형 묵주 (올리브나무)", 42, 3, "WH-01", "본사 창고", DateTime.UtcNow.AddHours(-1)),
        new("CTH-CRS-001", "벽걸이 십자가 (월넛)", 200, 15, "WH-01", "본사 창고", DateTime.UtcNow.AddMinutes(-45)),
        new("CTH-CRS-002", "탁상 십자가 (대리석)", 60, 8, "WH-02", "제2 창고", DateTime.UtcNow.AddHours(-2)),
        new("CTH-STT-001", "성모상 (30cm)", 35, 2, "WH-01", "본사 창고", DateTime.UtcNow.AddMinutes(-15)),
        new("CTH-STT-002", "예수상 (25cm)", 28, 4, "WH-01", "본사 창고", DateTime.UtcNow.AddHours(-1)),
        new("CTH-MED-001", "성 베네딕토 메달", 500, 20, "WH-02", "제2 창고", DateTime.UtcNow.AddMinutes(-10)),
        new("CTH-BIB-001", "성경 (가죽 표지)", 120, 0, "WH-01", "본사 창고", DateTime.UtcNow.AddHours(-3)),
        new("CTH-CND-001", "밀랍 초 (대형)", 300, 25, "WH-03", "생산 창고", DateTime.UtcNow.AddMinutes(-5)),
        new("MHN-DWJ-001", "전통 된장 (1kg)", 180, 12, "WH-A", "모현 창고", DateTime.UtcNow.AddMinutes(-20)),
        new("MHN-GCJ-001", "순창 고추장 (500g)", 250, 18, "WH-A", "모현 창고", DateTime.UtcNow.AddMinutes(-20)),
        new("MHN-GJG-001", "전통 간장 (1L)", 320, 8, "WH-A", "모현 창고", DateTime.UtcNow.AddHours(-1)),
        new("MHN-JRH-001", "지리환 (100g)", 95, 5, "WH-B", "모현 냉장 창고", DateTime.UtcNow.AddMinutes(-40)),
    };

    private static readonly List<MesProductInfo> _products = new()
    {
        new(1, "CTH-ROS-001", "5단 묵주 (자수정)", "완제품", "EA", true),
        new(2, "CTH-ROS-002", "5단 묵주 (장미)", "완제품", "EA", true),
        new(3, "CTH-ROS-003", "원형 묵주 (올리브나무)", "완제품", "EA", true),
        new(4, "CTH-CRS-001", "벽걸이 십자가 (월넛)", "완제품", "EA", true),
        new(5, "CTH-CRS-002", "탁상 십자가 (대리석)", "완제품", "EA", true),
        new(6, "CTH-STT-001", "성모상 (30cm)", "완제품", "EA", true),
        new(7, "CTH-STT-002", "예수상 (25cm)", "완제품", "EA", true),
        new(8, "CTH-MED-001", "성 베네딕토 메달", "완제품", "EA", true),
        new(9, "CTH-BIB-001", "성경 (가죽 표지)", "완제품", "EA", true),
        new(10, "CTH-CND-001", "밀랍 초 (대형)", "완제품", "EA", true),
        new(11, "MHN-DWJ-001", "전통 된장 (1kg)", "완제품", "KG", true),
        new(12, "MHN-GCJ-001", "순창 고추장 (500g)", "완제품", "KG", true),
        new(13, "MHN-GJG-001", "전통 간장 (1L)", "완제품", "L", true),
        new(14, "MHN-JRH-001", "지리환 (100g)", "완제품", "EA", true),
    };

    private static readonly Dictionary<string, MesOrderStatusResult> _orders = new();
    private static int _orderSeq = 1000;

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<List<MesInventoryItem>> GetInventoryAsync(CancellationToken ct = default)
        => Task.FromResult(_inventory.Select(i => i with { LastUpdated = DateTime.UtcNow }).ToList());

    public Task<MesSalesOrderResult> CreateSalesOrderAsync(MesSalesOrderRequest request, CancellationToken ct = default)
    {
        var mesOrderId = $"MES-{Interlocked.Increment(ref _orderSeq)}";
        _orders[request.OrderNo] = new MesOrderStatusResult(
            request.OrderNo, long.Parse(mesOrderId.Replace("MES-", "")),
            mesOrderId, "ProductionScheduled",
            DateTime.UtcNow, request.Items.Sum(i => i.OrderedQuantity * i.UnitPrice),
            DateTime.UtcNow);

        return Task.FromResult(new MesSalesOrderResult(true, mesOrderId, null));
    }

    public Task<MesSyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new MesSyncStatus(true, DateTime.UtcNow.AddMinutes(-5), _inventory.Count, null));

    public Task<MesReservationResult> ReserveInventoryAsync(MesReservationRequest request, CancellationToken ct = default)
    {
        var results = request.Items.Select(item =>
        {
            var inv = _inventory.FirstOrDefault(i => i.ProductCode == item.ProductCode);
            if (inv is null)
                return new MesReservationItemResult(item.ProductCode, false, "Product not found in MES");
            if (inv.AvailableQuantity < item.Quantity)
                return new MesReservationItemResult(item.ProductCode, false, $"Insufficient stock: {inv.AvailableQuantity} < {item.Quantity}");
            return new MesReservationItemResult(item.ProductCode, true, "Reserved");
        }).ToList();

        return Task.FromResult(new MesReservationResult(
            request.ShopOrderNo, request.RequestId, results.All(r => r.Success), results));
    }

    public Task<MesReservationResult> ReleaseInventoryAsync(MesReservationRequest request, CancellationToken ct = default)
    {
        var results = request.Items.Select(item =>
            new MesReservationItemResult(item.ProductCode, true, "Released")).ToList();

        return Task.FromResult(new MesReservationResult(
            request.ShopOrderNo, request.RequestId, true, results));
    }

    public Task<MesOrderStatusResult?> GetOrderStatusAsync(string shopOrderNo, CancellationToken ct = default)
    {
        _orders.TryGetValue(shopOrderNo, out var status);
        return Task.FromResult(status);
    }

    public Task<List<MesProductInfo>> GetMesProductsAsync(CancellationToken ct = default)
        => Task.FromResult(_products);
}
