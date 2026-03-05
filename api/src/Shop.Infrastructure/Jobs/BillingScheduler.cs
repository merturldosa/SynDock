using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Platform.Commands;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class BillingScheduler : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BillingScheduler> _logger;

    public BillingScheduler(IServiceProvider services, ILogger<BillingScheduler> logger)
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
                await ProcessBillingCycle(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in billing scheduler");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcessBillingCycle(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;
        var currentPeriod = now.ToString("yyyy-MM");

        // Find plans due for billing (NextBillingAt <= now AND Active)
        var duePlans = await db.TenantPlans
            .Where(p => p.BillingStatus == "Active"
                        && p.NextBillingAt != null
                        && p.NextBillingAt <= now
                        && p.MonthlyPrice > 0)
            .ToListAsync(ct);

        foreach (var plan in duePlans)
        {
            try
            {
                // Generate invoice for this billing period
                var result = await mediator.Send(new GenerateInvoiceCommand(plan.TenantId, currentPeriod), ct);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Invoice generated for tenant {TenantId}, period {Period}", plan.TenantId, currentPeriod);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate invoice for tenant {TenantId}", plan.TenantId);
            }
        }
    }
}
