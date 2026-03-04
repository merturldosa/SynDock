using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class BirthdayCouponJob : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<BirthdayCouponJob> _logger;

    public BirthdayCouponJob(IServiceProvider sp, ILogger<BirthdayCouponJob> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            // Run at 00:05 UTC daily
            var nextRun = now.Date.AddDays(now.Hour >= 0 && now.Minute >= 5 ? 1 : 0).AddMinutes(5);
            var delay = nextRun - now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                using var scope = _sp.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAutoCouponService>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<SynDock.Core.Interfaces.IUnitOfWork>();

                await service.IssueBirthdayCouponsAsync(stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Birthday coupon job completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Birthday coupon job failed");
            }
        }
    }
}
