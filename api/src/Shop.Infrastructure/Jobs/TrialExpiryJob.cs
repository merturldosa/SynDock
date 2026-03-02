using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class TrialExpiryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TrialExpiryJob> _logger;

    public TrialExpiryJob(IServiceProvider services, ILogger<TrialExpiryJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTrialExpiry(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trial expiry job");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessTrialExpiry(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();

        var now = DateTime.UtcNow;

        var expiredTrials = await db.TenantPlans
            .Where(p => p.BillingStatus == "Trial"
                        && p.TrialEndsAt != null
                        && p.TrialEndsAt <= now)
            .ToListAsync(ct);

        foreach (var plan in expiredTrials)
        {
            plan.BillingStatus = "Suspended";
            plan.UpdatedBy = "TrialExpiryJob";
            plan.UpdatedAt = now;

            _logger.LogInformation("Trial expired for tenant {TenantId}, suspended", plan.TenantId);
        }

        if (expiredTrials.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
