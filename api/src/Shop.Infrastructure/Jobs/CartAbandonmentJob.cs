using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class CartAbandonmentJob : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<CartAbandonmentJob> _logger;

    public CartAbandonmentJob(IServiceProvider sp, ILogger<CartAbandonmentJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Run at 06:00 UTC daily
            var nextRun = now.Date.AddDays(now.Hour >= 6 ? 1 : 0).AddHours(6);
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
                var unitOfWork = scope.ServiceProvider.GetRequiredService<SynDock.Core.Interfaces.IUnitOfWork>();

                var threshold = DateTime.UtcNow.AddHours(-24);
                var abandonedCarts = await db.Carts
                    .Include(c => c.Items).ThenInclude(i => i.Product)
                    .Include(c => c.User)
                    .Where(c => c.Items.Any()
                        && c.LastActivityAt != null
                        && c.LastActivityAt < threshold
                        && c.AbandonmentEmailSentAt == null)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var cart in abandonedCarts)
                {
                    try
                    {
                        var itemList = string.Join(", ", cart.Items.Select(i => i.Product?.Name ?? "item"));
                        var body = $"<h2>You left items in your cart!</h2><p>Hi {cart.User.Name}, you have items waiting: {itemList}. Complete your purchase today!</p>";
                        await emailService.SendAsync(cart.User.Email, "Don't forget your cart!", body, stoppingToken);
                        cart.AbandonmentEmailSentAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send abandonment email for cart {CartId}", cart.Id);
                    }
                }

                await unitOfWork.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Cart abandonment job completed. Processed {Count} carts", abandonedCarts.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Cart abandonment job failed");
            }
        }
    }
}
