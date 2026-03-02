using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Commands;

public record CreateCampaignCommand(string Title, string Content, string Target, DateTime? ScheduledAt) : IRequest<Result<int>>;

public class CreateCampaignCommandHandler : IRequestHandler<CreateCampaignCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateCampaignCommandHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<int>> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = new EmailCampaign
        {
            TenantId = _tenantContext.TenantId,
            Title = request.Title,
            Content = request.Content,
            Target = request.Target,
            Status = request.ScheduledAt.HasValue ? "Scheduled" : "Draft",
            ScheduledAt = request.ScheduledAt,
            CreatedBy = "Admin"
        };

        await _db.EmailCampaigns.AddAsync(campaign, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(campaign.Id);
    }
}

public record SendCampaignCommand(int CampaignId) : IRequest<Result<int>>;

public class SendCampaignCommandHandler : IRequestHandler<SendCampaignCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly IEmailService _emailService;

    public SendCampaignCommandHandler(IShopDbContext db, IEmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<Result<int>> Handle(SendCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _db.EmailCampaigns
            .FirstOrDefaultAsync(c => c.Id == request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result<int>.Failure("캠페인을 찾을 수 없습니다.");

        if (campaign.Status == "Sent")
            return Result<int>.Failure("이미 발송된 캠페인입니다.");

        campaign.Status = "Sending";
        await _db.SaveChangesAsync(cancellationToken);

        // Get target users based on segment
        var usersQuery = _db.Users.AsNoTracking()
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email));

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        usersQuery = campaign.Target switch
        {
            "new_users" => usersQuery.Where(u => u.CreatedAt >= thirtyDaysAgo),
            "vip" => usersQuery.Where(u => u.Role == "VIP" || u.Role == "Admin"),
            "inactive" => usersQuery.Where(u => u.CreatedAt < thirtyDaysAgo),
            _ => usersQuery // "all"
        };

        var emails = await usersQuery.Select(u => u.Email).ToListAsync(cancellationToken);

        var htmlBody = campaign.Content;
        var sentCount = 0;
        var failCount = 0;

        foreach (var email in emails)
        {
            try
            {
                await _emailService.SendAsync(email, campaign.Title, htmlBody, cancellationToken);
                sentCount++;
            }
            catch
            {
                failCount++;
            }
        }

        campaign.Status = "Sent";
        campaign.SentAt = DateTime.UtcNow;
        campaign.SentCount = sentCount;
        campaign.FailCount = failCount;
        campaign.UpdatedBy = "System";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(sentCount);
    }
}
