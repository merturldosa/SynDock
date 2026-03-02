using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class MesInventorySyncJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MesInventorySyncJob> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;

    public MesInventorySyncJob(IServiceProvider services, IConfiguration configuration, ILogger<MesInventorySyncJob> logger)
    {
        _services = services;
        _logger = logger;
        _enabled = configuration.GetValue<bool>("Mes:Enabled");
        var minutes = configuration.GetValue<int>("Mes:SyncIntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("MES inventory sync is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncInventory(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MES inventory sync");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task SyncInventory(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var mesClient = scope.ServiceProvider.GetRequiredService<IMesClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMesProductMapper>();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        if (!await mesClient.IsAvailableAsync(ct))
        {
            _logger.LogWarning("MES server is not available, skipping sync");
            return;
        }

        // MES JWT handles tenant context — single call retrieves all inventory
        var inventory = await mesClient.GetInventoryAsync(ct);
        if (inventory.Count == 0)
        {
            _logger.LogInformation("MES returned empty inventory, nothing to sync");
            return;
        }

        var syncedCount = 0;

        foreach (var item in inventory)
        {
            var productId = await mapper.GetShopProductIdAsync(item.ProductCode, ct);
            if (productId is null) continue;

            var variants = await db.ProductVariants
                .Where(v => v.ProductId == productId.Value && v.IsActive)
                .ToListAsync(ct);

            if (variants.Count == 0) continue;

            var mesStock = (int)Math.Round(item.AvailableQuantity);

            // Distribute MES stock across active variants proportionally
            var totalCurrentStock = variants.Sum(v => v.Stock);
            foreach (var variant in variants)
            {
                var ratio = totalCurrentStock > 0
                    ? (double)variant.Stock / totalCurrentStock
                    : 1.0 / variants.Count;
                variant.Stock = (int)Math.Round(mesStock * ratio);
            }

            syncedCount++;
        }

        if (syncedCount > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("MES inventory synced: {Count} products updated", syncedCount);
        }

        // Record sync timestamp
        await cache.SetStringAsync("mes:lastSync", DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) }, ct);
        await cache.SetStringAsync("mes:syncedCount", syncedCount.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) }, ct);
    }
}
