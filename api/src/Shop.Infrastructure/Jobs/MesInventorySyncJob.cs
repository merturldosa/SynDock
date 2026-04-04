using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class MesInventorySyncJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MesInventorySyncJob> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;
    private readonly int _retryCount;
    private readonly int _retryDelayMs;

    public MesInventorySyncJob(IServiceProvider services, IConfiguration configuration, ILogger<MesInventorySyncJob> logger)
    {
        _services = services;
        _logger = logger;
        var mesMode = configuration["Mes:Enabled"]?.ToLower();
        _enabled = mesMode == "true" || mesMode == "demo";
        var minutes = configuration.GetValue<int>("Mes:SyncIntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(minutes);
        _retryCount = configuration.GetValue<int>("Mes:SyncRetryCount", 2);
        _retryDelayMs = configuration.GetValue<int>("Mes:SyncRetryDelayMs", 200);
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
            catch (Exception ex) when (ex is not OperationCanceledException)
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
        var adminNotifier = scope.ServiceProvider.GetRequiredService<IAdminDashboardNotifier>();

        var sw = Stopwatch.StartNew();
        var history = new MesSyncHistory
        {
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };
        db.MesSyncHistories.Add(history);
        await db.SaveChangesAsync(ct);

        var errors = new List<object>();
        var conflicts = new List<object>();
        var syncedCount = 0;
        var failedCount = 0;
        var skippedCount = 0;

        try
        {
            if (!await mesClient.IsAvailableAsync(ct))
            {
                _logger.LogWarning("MES server is not available, skipping sync");
                history.Status = "Failed";
                history.ErrorDetailsJson = JsonSerializer.Serialize(new[] { new { reason = "MES server unavailable" } });
                sw.Stop();
                history.ElapsedMs = sw.ElapsedMilliseconds;
                history.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return;
            }

            var inventory = await mesClient.GetInventoryAsync(ct);
            if (inventory.Count == 0)
            {
                _logger.LogInformation("MES returned empty inventory, nothing to sync");
                history.Status = "Completed";
                sw.Stop();
                history.ElapsedMs = sw.ElapsedMilliseconds;
                history.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                return;
            }

            foreach (var item in inventory)
            {
                var productId = await mapper.GetShopProductIdAsync(item.ProductCode, ct);
                if (productId is null)
                {
                    skippedCount++;
                    continue;
                }

                var success = false;
                for (var attempt = 0; attempt <= _retryCount; attempt++)
                {
                    try
                    {
                        await SyncSingleProduct(db, cache, productId.Value, item, conflicts, ct);
                        success = true;
                        syncedCount++;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (attempt < _retryCount)
                        {
                            var delay = _retryDelayMs * (int)Math.Pow(2, attempt);
                            _logger.LogWarning(ex, "Retry {Attempt}/{Max} for product {ProductId} in {Delay}ms",
                                attempt + 1, _retryCount, productId.Value, delay);
                            await Task.Delay(delay, ct);
                        }
                        else
                        {
                            failedCount++;
                            errors.Add(new { productId = productId.Value, productCode = item.ProductCode, error = ex.Message });
                            _logger.LogError(ex, "Failed to sync product {ProductId} after {Attempts} retries",
                                productId.Value, _retryCount);
                        }
                    }
                }

                if (!success && failedCount == 0)
                {
                    skippedCount++;
                }
            }

            if (syncedCount > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("MES inventory synced: {Synced} products updated, {Failed} failed, {Skipped} skipped",
                    syncedCount, failedCount, skippedCount);
            }

            history.Status = failedCount > 0 ? "CompletedWithErrors" : "Completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during MES inventory sync");
            history.Status = "Failed";
            errors.Add(new { reason = "Critical sync error", error = ex.Message });
        }
        finally
        {
            sw.Stop();
            history.ElapsedMs = sw.ElapsedMilliseconds;
            history.CompletedAt = DateTime.UtcNow;
            history.SuccessCount = syncedCount;
            history.FailedCount = failedCount;
            history.SkippedCount = skippedCount;
            if (errors.Count > 0)
                history.ErrorDetailsJson = JsonSerializer.Serialize(errors);
            if (conflicts.Count > 0)
                history.ConflictDetailsJson = JsonSerializer.Serialize(conflicts);

            await db.SaveChangesAsync(ct);

            // Record sync timestamp in Redis
            await cache.SetStringAsync("mes:lastSync", DateTime.UtcNow.ToString("O"),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) }, ct);
            await cache.SetStringAsync("mes:syncedCount", syncedCount.ToString(),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) }, ct);

            // SignalR notification (tenantId 0 = all tenants for system-level sync)
            try
            {
                await adminNotifier.NotifyMesSyncCompleted(0, syncedCount, failedCount, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send MES sync notification");
            }
        }
    }

    private static async Task SyncSingleProduct(
        ShopDbContext db,
        IDistributedCache cache,
        int productId,
        MesInventoryItem item,
        List<object> conflicts,
        CancellationToken ct)
    {
        var variants = await db.ProductVariants
            .Where(v => v.ProductId == productId && v.IsActive)
            .ToListAsync(ct);

        if (variants.Count == 0) return;

        var mesStock = (int)Math.Round(item.AvailableQuantity);
        var totalCurrentStock = variants.Sum(v => v.Stock);

        // Conflict detection: check if shop stock changed manually since last sync
        var snapshotKey = $"mes:lastStockSnapshot:{productId}";
        var snapshotJson = await cache.GetStringAsync(snapshotKey, ct);
        if (snapshotJson is not null)
        {
            var lastSnapshot = int.TryParse(snapshotJson, out var snap) ? snap : -1;
            if (lastSnapshot >= 0 && totalCurrentStock != lastSnapshot)
            {
                conflicts.Add(new
                {
                    productId,
                    productCode = item.ProductCode,
                    lastSnapshotStock = lastSnapshot,
                    currentShopStock = totalCurrentStock,
                    mesStock,
                    resolution = "MES stock applied (overwrite)"
                });
            }
        }

        // Distribute MES stock across active variants proportionally
        foreach (var variant in variants)
        {
            var ratio = totalCurrentStock > 0
                ? (double)variant.Stock / totalCurrentStock
                : 1.0 / variants.Count;
            variant.Stock = (int)Math.Round(mesStock * ratio);
        }

        // Save snapshot for next conflict detection
        var newTotal = variants.Sum(v => v.Stock);
        await cache.SetStringAsync(snapshotKey, newTotal.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) }, ct);

        // Invalidate product cache
        await cache.RemoveAsync($"product:{productId}", ct);
        await cache.RemoveAsync($"product:variants:{productId}", ct);
    }
}
