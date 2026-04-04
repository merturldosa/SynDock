using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class LotExpirationAlertJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LotExpirationAlertJob> _logger;

    public LotExpirationAlertJob(IServiceProvider services, ILogger<LotExpirationAlertJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CheckExpiringLots(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiring lots");
            }
            await Task.Delay(TimeSpan.FromHours(24), ct);
        }
    }

    private async Task CheckExpiringLots(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var warningDate = DateTime.UtcNow.AddDays(30);
        var now = DateTime.UtcNow;

        // Mark expired lots
        var expiredLots = await db.LotTrackings
            .Where(l => l.Status == "Available" && l.ExpiryDate != null && l.ExpiryDate <= now)
            .ToListAsync(ct);

        foreach (var lot in expiredLots)
        {
            lot.Status = "Expired";
            lot.UpdatedBy = "LotExpirationJob";
            lot.UpdatedAt = now;
        }

        if (expiredLots.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Marked {Count} lots as expired", expiredLots.Count);
        }

        // Count expiring soon (for logging)
        var expiringCount = await db.LotTrackings
            .CountAsync(l => l.Status == "Available" && l.ExpiryDate != null && l.ExpiryDate > now && l.ExpiryDate <= warningDate, ct);

        if (expiringCount > 0)
            _logger.LogInformation("{Count} lots expiring within 30 days", expiringCount);
    }
}
