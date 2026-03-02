using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record CampaignDto(
    int Id,
    string Title,
    string Target,
    string Status,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    int SentCount,
    int FailCount,
    DateTime CreatedAt);

public record GetCampaignsQuery : IRequest<Result<List<CampaignDto>>>;

public class GetCampaignsQueryHandler : IRequestHandler<GetCampaignsQuery, Result<List<CampaignDto>>>
{
    private readonly IShopDbContext _db;

    public GetCampaignsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<CampaignDto>>> Handle(GetCampaignsQuery request, CancellationToken cancellationToken)
    {
        var campaigns = await _db.EmailCampaigns.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CampaignDto(
                c.Id,
                c.Title,
                c.Target,
                c.Status,
                c.ScheduledAt,
                c.SentAt,
                c.SentCount,
                c.FailCount,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<CampaignDto>>.Success(campaigns);
    }
}
