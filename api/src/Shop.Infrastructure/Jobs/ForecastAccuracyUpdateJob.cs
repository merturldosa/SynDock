using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class ForecastAccuracyUpdateJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ForecastAccuracyUpdateJob> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;

    public ForecastAccuracyUpdateJob(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<ForecastAccuracyUpdateJob> logger)
    {
        _services = services;
        _logger = logger;
        _enabled = configuration.GetValue("Forecast:AccuracyTracking:Enabled", true);
        var hours = configuration.GetValue("Forecast:AccuracyTracking:IntervalHours", 24);
        _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Forecast accuracy tracking is disabled");
            return;
        }

        // Wait until 02:00 UTC for first run
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(2);
        if (nextRun <= now) nextRun = nextRun.AddDays(1);
        var initialDelay = nextRun - now;
        _logger.LogInformation("Forecast accuracy job will start at {NextRun} (in {Delay})", nextRun, initialDelay);

        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAccuracyUpdate(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in forecast accuracy update job");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAccuracyUpdate(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var forecastService = scope.ServiceProvider.GetRequiredService<IDemandForecastService>();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();

        _logger.LogInformation("Starting forecast accuracy update");

        // 1. Update actual demand for past forecasts
        await forecastService.UpdateActualDemandAsync(ct);

        // 2. Record new forecasts for active products with stock
        var productIds = await db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive && v.Stock > 0)
            .Select(v => v.ProductId)
            .Distinct()
            .ToListAsync(ct);

        var recorded = 0;
        foreach (var pid in productIds)
        {
            try
            {
                await forecastService.RecordForecastAsync(pid, ct);
                recorded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record forecast for product {ProductId}", pid);
            }
        }

        _logger.LogInformation("Forecast accuracy update completed: {Recorded} products recorded", recorded);
    }
}
