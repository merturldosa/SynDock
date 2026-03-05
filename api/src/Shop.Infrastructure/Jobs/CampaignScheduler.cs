using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Jobs;

public class CampaignScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CampaignScheduler> _logger;

    public CampaignScheduler(IServiceProvider serviceProvider, ILogger<CampaignScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledCampaigns(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Campaign scheduler error");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessScheduledCampaigns(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IShopDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;
        var campaigns = await db.EmailCampaigns
            .IgnoreQueryFilters()
            .Where(c => c.Status == "Scheduled" && c.ScheduledAt <= now)
            .ToListAsync(ct);

        foreach (var campaign in campaigns)
        {
            try
            {
                _logger.LogInformation("Scheduled campaign sending started: {CampaignId} ({Title})", campaign.Id, campaign.Title);

                campaign.Status = "Sending";
                await db.SaveChangesAsync(ct);

                if (campaign.IsAbTest)
                {
                    await SendAbTestCampaign(db, emailService, campaign, ct);
                }
                else
                {
                    await SendStandardCampaign(db, emailService, campaign, ct);
                }

                campaign.Status = "Sent";
                campaign.SentAt = DateTime.UtcNow;
                campaign.UpdatedBy = "CampaignScheduler";
                campaign.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Campaign sending completed: {CampaignId} (sent: {Sent}, failed: {Failed})",
                    campaign.Id, campaign.SentCount, campaign.FailCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Campaign sending failed: {CampaignId}", campaign.Id);
                campaign.Status = "Failed";
                campaign.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static async Task SendStandardCampaign(IShopDbContext db, IEmailService emailService, EmailCampaign campaign, CancellationToken ct)
    {
        var emails = await GetTargetEmails(db, campaign.TenantId, campaign.Target, ct);
        var sentCount = 0;
        var failCount = 0;

        foreach (var email in emails)
        {
            try
            {
                await emailService.SendAsync(email, campaign.Title, campaign.Content, ct);
                sentCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failCount++;
            }
        }

        campaign.SentCount = sentCount;
        campaign.FailCount = failCount;
    }

    private static async Task SendAbTestCampaign(IShopDbContext db, IEmailService emailService, EmailCampaign campaign, CancellationToken ct)
    {
        var variants = await db.CampaignVariants
            .IgnoreQueryFilters()
            .Where(v => v.CampaignId == campaign.Id)
            .OrderBy(v => v.VariantName)
            .ToListAsync(ct);

        if (variants.Count == 0) return;

        var emails = await GetTargetEmails(db, campaign.TenantId, campaign.Target, ct);
        var shuffled = emails.OrderBy(_ => Random.Shared.Next()).ToList();

        var totalSent = 0;
        var totalFail = 0;
        var offset = 0;

        foreach (var variant in variants)
        {
            var count = (int)(shuffled.Count * variant.TrafficPercent / 100.0);
            if (variant == variants.Last()) count = shuffled.Count - offset; // remainder to last variant
            var subset = shuffled.Skip(offset).Take(count).ToList();
            offset += count;

            var sentCount = 0;
            var failCount = 0;

            foreach (var email in subset)
            {
                try
                {
                    await emailService.SendAsync(email, variant.SubjectLine, variant.Content, ct);
                    sentCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failCount++;
                }
            }

            variant.SentCount = sentCount;
            totalSent += sentCount;
            totalFail += failCount;
        }

        campaign.SentCount = totalSent;
        campaign.FailCount = totalFail;
    }

    private static async Task<List<string>> GetTargetEmails(IShopDbContext db, int tenantId, string target, CancellationToken ct)
    {
        var usersQuery = db.Users.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.IsActive && !string.IsNullOrEmpty(u.Email));

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        usersQuery = target switch
        {
            "new_users" => usersQuery.Where(u => u.CreatedAt >= thirtyDaysAgo),
            "vip" => usersQuery.Where(u => u.Role == "VIP" || u.Role == "Admin"),
            "inactive" => usersQuery.Where(u => u.CreatedAt < thirtyDaysAgo),
            _ => usersQuery
        };

        return await usersQuery.Select(u => u.Email).ToListAsync(ct);
    }
}
