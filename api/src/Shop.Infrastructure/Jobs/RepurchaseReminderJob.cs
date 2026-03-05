using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class RepurchaseReminderJob : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<RepurchaseReminderJob> _logger;

    public RepurchaseReminderJob(IServiceProvider sp, ILogger<RepurchaseReminderJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Run at 09:00 UTC daily
            var nextRun = now.Date.AddDays(now.Hour >= 9 ? 1 : 0).AddHours(9);
            var delay = nextRun - now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

                // Find users who ordered 30-60 days ago but haven't ordered since
                var candidates = await db.Orders
                    .Where(o => o.CreatedAt >= sixtyDaysAgo && o.CreatedAt <= thirtyDaysAgo)
                    .Select(o => new { o.UserId, o.User.Email, o.User.Name })
                    .Distinct()
                    .Take(100)
                    .ToListAsync(stoppingToken);

                var recentBuyers = await db.Orders
                    .Where(o => o.CreatedAt > thirtyDaysAgo)
                    .Select(o => o.UserId)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                var targets = candidates.Where(c => !recentBuyers.Contains(c.UserId)).ToList();
                var sentCount = 0;

                foreach (var target in targets)
                {
                    try
                    {
                        var body = $"<h2>We miss you!</h2><p>Hi {target.Name}, it's been a while since your last order. Check out our latest products and special offers!</p>";
                        await emailService.SendAsync(target.Email, "We miss you! Come back for great deals", body, stoppingToken);
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send repurchase reminder to user {UserId}", target.UserId);
                    }
                }

                _logger.LogInformation("Repurchase reminder job completed. Sent {Count} emails", sentCount);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Repurchase reminder job failed");
            }
        }
    }
}
