using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Integration;

public class NullMesClient : IMesClient
{
    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(false);

    public Task<List<MesInventoryItem>> GetInventoryAsync(CancellationToken ct = default)
        => Task.FromResult(new List<MesInventoryItem>());

    public Task<MesSalesOrderResult> CreateSalesOrderAsync(MesSalesOrderRequest request, CancellationToken ct = default)
        => Task.FromResult(new MesSalesOrderResult(false, null, "MES integration is not configured"));

    public Task<MesSyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
        => Task.FromResult(new MesSyncStatus(false, null, 0, "MES integration is not configured"));
}
