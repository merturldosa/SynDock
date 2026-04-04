using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class MarketplaceStockSyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MarketplaceStockSyncJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    public MarketplaceStockSyncJob(IServiceScopeFactory scopeFactory, ILogger<MarketplaceStockSyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketplaceStockSyncJob started (interval: {Interval})", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await RunSyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarketplaceStockSyncJob error");
            }
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
        var marketplaceService = scope.ServiceProvider.GetRequiredService<IMarketplaceService>();

        // Get all tenants with active marketplace connections
        var activeConnections = await db.MarketplaceConnections
            .Where(c => c.Status == "Connected")
            .Select(c => new { c.TenantId, c.Id })
            .ToListAsync(ct);

        if (!activeConnections.Any()) return;

        var tenantIds = activeConnections.Select(c => c.TenantId).Distinct().ToList();
        _logger.LogInformation("Marketplace sync: {TenantCount} tenants, {ConnectionCount} connections",
            tenantIds.Count, activeConnections.Count);

        foreach (var tenantId in tenantIds)
        {
            try
            {
                // Sync stock levels to all connected marketplaces
                await marketplaceService.SyncStockAsync(tenantId, null, ct);

                // Sync orders from marketplaces back to SynDock
                var tenantConnections = activeConnections.Where(c => c.TenantId == tenantId);
                foreach (var conn in tenantConnections)
                {
                    await marketplaceService.SyncOrdersAsync(tenantId, conn.Id, ct);
                }

                _logger.LogDebug("Marketplace sync completed for tenant {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Marketplace sync failed for tenant {TenantId}", tenantId);
            }
        }

        _logger.LogInformation("Marketplace sync cycle completed");
    }
}
