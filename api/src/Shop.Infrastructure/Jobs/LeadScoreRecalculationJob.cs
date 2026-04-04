using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class LeadScoreRecalculationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LeadScoreRecalculationJob> _logger;

    public LeadScoreRecalculationJob(IServiceProvider services, ILogger<LeadScoreRecalculationJob> logger)
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
                using var scope = _services.CreateScope();
                var crm = scope.ServiceProvider.GetRequiredService<ICrmService>();
                // Recalculate for all tenants - tenant filter is applied by DbContext
                await crm.RecalculateAllLeadScoresAsync(0, ct);
                _logger.LogInformation("Lead scores recalculated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating lead scores");
            }
            await Task.Delay(TimeSpan.FromDays(7), ct);
        }
    }
}
