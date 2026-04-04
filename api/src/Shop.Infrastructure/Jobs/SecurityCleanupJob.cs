using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class SecurityCleanupJob : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SecurityCleanupJob> _logger;

    public SecurityCleanupJob(IServiceProvider sp, ILogger<SecurityCleanupJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SecurityCleanupJob error");
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
        var now = DateTime.UtcNow;

        // 1. Remove expired IP blocks
        var expiredBlocks = await db.BlockedIps
            .Where(b => b.IsActive && b.ExpiresAt != null && b.ExpiresAt <= now)
            .ToListAsync(ct);
        foreach (var block in expiredBlocks)
            block.IsActive = false;

        // 2. Remove expired account lockouts (re-activate users)
        var expiredLockouts = await db.AccountLockouts
            .Where(a => a.Status == "Locked" && a.LockedUntil != null && a.LockedUntil <= now)
            .ToListAsync(ct);
        foreach (var lockout in expiredLockouts)
        {
            lockout.Status = "Unlocked";
            lockout.UnlockedAt = now;
            lockout.UnlockedBy = "SecurityCleanupJob";

            // Re-activate user
            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == lockout.UserId, ct);
            if (user != null && !user.IsActive)
            {
                user.IsActive = true;
                user.UpdatedBy = "SecurityCleanupJob";
                user.UpdatedAt = now;
            }
        }

        // 3. Clean old security events (keep 90 days)
        var cutoff = now.AddDays(-90);
        var oldEvents = await db.SecurityEvents
            .Where(e => e.CreatedAt < cutoff)
            .ToListAsync(ct);
        if (oldEvents.Count > 0)
            db.SecurityEvents.RemoveRange(oldEvents);

        await db.SaveChangesAsync(ct);

        if (expiredBlocks.Count > 0 || expiredLockouts.Count > 0 || oldEvents.Count > 0)
        {
            _logger.LogInformation(
                "SecurityCleanupJob: {ExpiredBlocks} IP blocks expired, {ExpiredLockouts} lockouts expired, {OldEvents} old events purged",
                expiredBlocks.Count, expiredLockouts.Count, oldEvents.Count);
        }
    }
}
