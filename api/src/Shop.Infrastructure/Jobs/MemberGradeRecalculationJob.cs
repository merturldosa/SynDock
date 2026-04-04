using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Jobs;

public class MemberGradeRecalculationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MemberGradeRecalculationJob> _logger;

    public MemberGradeRecalculationJob(IServiceProvider services, ILogger<MemberGradeRecalculationJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MemberGradeRecalculationJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run daily at 3 AM UTC
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1).AddHours(3);
                if (now.Hour < 3) nextRun = now.Date.AddHours(3);

                var delay = nextRun - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                await RecalculateAllGradesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MemberGradeRecalculationJob");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task RecalculateAllGradesAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting daily member grade recalculation");

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
        var social = scope.ServiceProvider.GetRequiredService<ISocialCommerceService>();

        // Get all tenants
        var tenants = await db.Tenants.Where(t => t.IsActive).Select(t => t.Id).ToListAsync(ct);

        var totalRecalculated = 0;
        var totalUpgraded = 0;

        foreach (var tenantId in tenants)
        {
            try
            {
                // Get all member grades due for review (or all if no NextReviewAt set)
                var grades = await db.MemberGrades
                    .IgnoreQueryFilters()
                    .Where(g => g.TenantId == tenantId && (g.NextReviewAt == null || g.NextReviewAt <= DateTime.UtcNow))
                    .ToListAsync(ct);

                // Also find users without a grade yet (active users with at least 1 order)
                var usersWithGrades = grades.Select(g => g.UserId).ToHashSet();
                var usersWithoutGrades = await db.Orders
                    .IgnoreQueryFilters()
                    .Where(o => o.TenantId == tenantId && !usersWithGrades.Contains(o.UserId))
                    .Select(o => o.UserId)
                    .Distinct()
                    .ToListAsync(ct);

                var allUserIds = grades.Select(g => g.UserId)
                    .Concat(usersWithoutGrades)
                    .Distinct()
                    .ToList();

                foreach (var userId in allUserIds)
                {
                    try
                    {
                        var oldGrade = grades.FirstOrDefault(g => g.UserId == userId)?.Grade ?? "Bronze";
                        var updated = await social.RecalculateGradeAsync(tenantId, userId, ct);
                        totalRecalculated++;

                        if (updated.Grade != oldGrade && GradeRank(updated.Grade) > GradeRank(oldGrade))
                            totalUpgraded++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to recalculate grade for tenant {TenantId} user {UserId}", tenantId, userId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process tenant {TenantId} grades", tenantId);
            }
        }

        _logger.LogInformation("Grade recalculation complete: {Recalculated} recalculated, {Upgraded} upgraded",
            totalRecalculated, totalUpgraded);
    }

    private static int GradeRank(string grade) => grade switch
    {
        "Bronze" => 0,
        "Silver" => 1,
        "Gold" => 2,
        "Platinum" => 3,
        "Diamond" => 4,
        "VIP" => 5,
        _ => 0
    };
}
