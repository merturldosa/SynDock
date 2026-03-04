using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Commands;

public record CreateAbTestCampaignCommand(
    string Title,
    string Target,
    DateTime? ScheduledAt,
    string SubjectLineA,
    string ContentA,
    string SubjectLineB,
    string ContentB,
    int TrafficPercentA = 50
) : IRequest<Result<int>>;

public class CreateAbTestCampaignCommandHandler : IRequestHandler<CreateAbTestCampaignCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateAbTestCampaignCommandHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<int>> Handle(CreateAbTestCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = new EmailCampaign
        {
            TenantId = _tenantContext.TenantId,
            Title = request.Title,
            Content = request.ContentA,
            Target = request.Target,
            IsAbTest = true,
            Status = request.ScheduledAt.HasValue ? "Scheduled" : "Draft",
            ScheduledAt = request.ScheduledAt,
            CreatedBy = "Admin"
        };

        await _db.EmailCampaigns.AddAsync(campaign, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var variantA = new CampaignVariant
        {
            TenantId = _tenantContext.TenantId,
            CampaignId = campaign.Id,
            VariantName = "A",
            SubjectLine = request.SubjectLineA,
            Content = request.ContentA,
            TrafficPercent = request.TrafficPercentA,
            CreatedBy = "Admin"
        };

        var variantB = new CampaignVariant
        {
            TenantId = _tenantContext.TenantId,
            CampaignId = campaign.Id,
            VariantName = "B",
            SubjectLine = request.SubjectLineB,
            Content = request.ContentB,
            TrafficPercent = 100 - request.TrafficPercentA,
            CreatedBy = "Admin"
        };

        await _db.CampaignVariants.AddAsync(variantA, cancellationToken);
        await _db.CampaignVariants.AddAsync(variantB, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(campaign.Id);
    }
}

public record RecordCampaignEventCommand(int CampaignId, int? VariantId, int UserId, string EventType, string? LinkUrl = null) : IRequest<Result<bool>>;

public class RecordCampaignEventCommandHandler : IRequestHandler<RecordCampaignEventCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public RecordCampaignEventCommandHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<bool>> Handle(RecordCampaignEventCommand request, CancellationToken cancellationToken)
    {
        var metric = new CampaignMetric
        {
            TenantId = _tenantContext.TenantId,
            CampaignId = request.CampaignId,
            VariantId = request.VariantId,
            UserId = request.UserId,
            EventType = request.EventType,
            LinkUrl = request.LinkUrl,
            CreatedBy = "System"
        };

        await _db.CampaignMetrics.AddAsync(metric, cancellationToken);

        // Update campaign aggregate counts
        var campaign = await _db.EmailCampaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);
        if (campaign is not null)
        {
            switch (request.EventType)
            {
                case "Opened": campaign.OpenCount++; break;
                case "Clicked": campaign.ClickCount++; break;
                case "Converted": campaign.ConversionCount++; break;
            }
        }

        // Update variant counts
        if (request.VariantId.HasValue)
        {
            var variant = await _db.CampaignVariants.FirstOrDefaultAsync(v => v.Id == request.VariantId.Value, cancellationToken);
            if (variant is not null)
            {
                switch (request.EventType)
                {
                    case "Opened": variant.OpenCount++; break;
                    case "Clicked": variant.ClickCount++; break;
                    case "Converted": variant.ConversionCount++; break;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
