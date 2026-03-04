using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Platform.Commands;
using Shop.Infrastructure.Data;

namespace Shop.Infrastructure.Jobs;

public class SettlementScheduler : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SettlementScheduler> _logger;

    public SettlementScheduler(IServiceProvider services, ILogger<SettlementScheduler> logger)
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
                await CreatePendingSettlements(stoppingToken);
                await ProcessReadySettlements(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in settlement scheduler");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    /// <summary>
    /// 정산 주기에 맞는 테넌트의 정산 배치 생성
    /// </summary>
    private async Task CreatePendingSettlements(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;
        var today = (int)now.DayOfWeek;

        // 정산 주기에 해당하는 테넌트별 설정 조회
        var settings = await db.CommissionSettings
            .AsNoTracking()
            .Where(cs => cs.ProductId == null && cs.CategoryId == null) // 테넌트 기본 설정만
            .ToListAsync(ct);

        foreach (var setting in settings)
        {
            try
            {
                var shouldSettle = setting.SettlementCycle switch
                {
                    "Weekly" => today == setting.SettlementDayOfWeek,
                    "Biweekly" => today == setting.SettlementDayOfWeek && now.Day <= 14,
                    "Monthly" => now.Day == 1,
                    _ => false
                };

                if (!shouldSettle)
                    continue;

                // 정산 기간 계산
                var (periodStart, periodEnd) = setting.SettlementCycle switch
                {
                    "Weekly" => (now.AddDays(-7).Date, now.Date),
                    "Biweekly" => (now.AddDays(-14).Date, now.Date),
                    "Monthly" => (now.AddMonths(-1).Date, now.Date),
                    _ => (now.AddDays(-7).Date, now.Date)
                };

                var result = await mediator.Send(
                    new CreateSettlementCommand(setting.TenantId, periodStart, periodEnd), ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Settlement created for tenant {TenantId}, period {Start:yyyy-MM-dd} ~ {End:yyyy-MM-dd}, id={SettlementId}",
                        setting.TenantId, periodStart, periodEnd, result.Data);
                }
                else
                {
                    _logger.LogDebug(
                        "Settlement skipped for tenant {TenantId}: {Reason}",
                        setting.TenantId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create settlement for tenant {TenantId}", setting.TenantId);
            }
        }
    }

    /// <summary>
    /// Ready 상태 정산을 자동으로 이체 처리 (ITransferService 통해)
    /// </summary>
    private async Task ProcessReadySettlements(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var readySettlements = await db.Settlements
            .Where(s => s.Status == "Ready")
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var settlementId in readySettlements)
        {
            try
            {
                var result = await mediator.Send(
                    new ProcessSettlementCommand(settlementId, null, "SettlementScheduler"), ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Settlement #{SettlementId} processed successfully", settlementId);
                }
                else
                {
                    _logger.LogWarning("Settlement #{SettlementId} processing failed: {Reason}",
                        settlementId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process settlement #{SettlementId}", settlementId);
            }
        }
    }
}
