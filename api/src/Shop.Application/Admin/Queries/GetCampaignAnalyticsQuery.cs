using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record CampaignAnalyticsDto(
    int Id,
    string Title,
    string Target,
    string Status,
    bool IsAbTest,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    int SentCount,
    int OpenCount,
    int ClickCount,
    int ConversionCount,
    decimal Revenue,
    double OpenRate,
    double ClickRate,
    double ConversionRate,
    List<CampaignVariantDto>? Variants);

public record CampaignVariantDto(
    int Id,
    string VariantName,
    string SubjectLine,
    int TrafficPercent,
    int SentCount,
    int OpenCount,
    int ClickCount,
    int ConversionCount,
    decimal Revenue,
    double OpenRate,
    double ClickRate,
    double ConversionRate,
    bool IsWinner);

public record GetCampaignAnalyticsQuery(int CampaignId) : IRequest<Result<CampaignAnalyticsDto>>;

public class GetCampaignAnalyticsQueryHandler : IRequestHandler<GetCampaignAnalyticsQuery, Result<CampaignAnalyticsDto>>
{
    private readonly IShopDbContext _db;

    public GetCampaignAnalyticsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CampaignAnalyticsDto>> Handle(GetCampaignAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _db.EmailCampaigns.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result<CampaignAnalyticsDto>.Failure("Campaign not found.");

        List<CampaignVariantDto>? variants = null;

        if (campaign.IsAbTest)
        {
            var variantEntities = await _db.CampaignVariants.AsNoTracking()
                .Where(v => v.CampaignId == campaign.Id)
                .OrderBy(v => v.VariantName)
                .ToListAsync(cancellationToken);

            variants = variantEntities.Select(v => new CampaignVariantDto(
                v.Id,
                v.VariantName,
                v.SubjectLine,
                v.TrafficPercent,
                v.SentCount,
                v.OpenCount,
                v.ClickCount,
                v.ConversionCount,
                v.Revenue,
                v.SentCount > 0 ? Math.Round((double)v.OpenCount / v.SentCount * 100, 1) : 0,
                v.OpenCount > 0 ? Math.Round((double)v.ClickCount / v.OpenCount * 100, 1) : 0,
                v.ClickCount > 0 ? Math.Round((double)v.ConversionCount / v.ClickCount * 100, 1) : 0,
                v.IsWinner
            )).ToList();
        }

        var dto = new CampaignAnalyticsDto(
            campaign.Id,
            campaign.Title,
            campaign.Target,
            campaign.Status,
            campaign.IsAbTest,
            campaign.ScheduledAt,
            campaign.SentAt,
            campaign.SentCount,
            campaign.OpenCount,
            campaign.ClickCount,
            campaign.ConversionCount,
            campaign.Revenue,
            campaign.SentCount > 0 ? Math.Round((double)campaign.OpenCount / campaign.SentCount * 100, 1) : 0,
            campaign.OpenCount > 0 ? Math.Round((double)campaign.ClickCount / campaign.OpenCount * 100, 1) : 0,
            campaign.ClickCount > 0 ? Math.Round((double)campaign.ConversionCount / campaign.ClickCount * 100, 1) : 0,
            variants
        );

        return Result<CampaignAnalyticsDto>.Success(dto);
    }
}

public record CampaignSummaryDto(
    int TotalCampaigns,
    int SentCampaigns,
    int TotalSent,
    int TotalOpened,
    int TotalClicked,
    int TotalConverted,
    decimal TotalRevenue,
    double AvgOpenRate,
    double AvgClickRate,
    double AvgConversionRate);

public record GetCampaignSummaryQuery : IRequest<Result<CampaignSummaryDto>>;

public class GetCampaignSummaryQueryHandler : IRequestHandler<GetCampaignSummaryQuery, Result<CampaignSummaryDto>>
{
    private readonly IShopDbContext _db;

    public GetCampaignSummaryQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CampaignSummaryDto>> Handle(GetCampaignSummaryQuery request, CancellationToken cancellationToken)
    {
        var campaigns = await _db.EmailCampaigns.AsNoTracking().ToListAsync(cancellationToken);

        var totalCampaigns = campaigns.Count;
        var sentCampaigns = campaigns.Count(c => c.Status == "Sent");
        var totalSent = campaigns.Sum(c => c.SentCount);
        var totalOpened = campaigns.Sum(c => c.OpenCount);
        var totalClicked = campaigns.Sum(c => c.ClickCount);
        var totalConverted = campaigns.Sum(c => c.ConversionCount);
        var totalRevenue = campaigns.Sum(c => c.Revenue);

        var sentWithData = campaigns.Where(c => c.SentCount > 0).ToList();
        var avgOpenRate = sentWithData.Count > 0
            ? Math.Round(sentWithData.Average(c => (double)c.OpenCount / c.SentCount * 100), 1) : 0;
        var avgClickRate = sentWithData.Where(c => c.OpenCount > 0).ToList() is { Count: > 0 } clickable
            ? Math.Round(clickable.Average(c => (double)c.ClickCount / c.OpenCount * 100), 1) : 0;
        var avgConversionRate = sentWithData.Where(c => c.ClickCount > 0).ToList() is { Count: > 0 } convertible
            ? Math.Round(convertible.Average(c => (double)c.ConversionCount / c.ClickCount * 100), 1) : 0;

        return Result<CampaignSummaryDto>.Success(new CampaignSummaryDto(
            totalCampaigns, sentCampaigns, totalSent, totalOpened, totalClicked,
            totalConverted, totalRevenue, avgOpenRate, avgClickRate, avgConversionRate
        ));
    }
}
