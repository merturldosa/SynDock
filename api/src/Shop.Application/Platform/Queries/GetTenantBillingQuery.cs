using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Queries;

public record TenantBillingDto(
    int TenantId,
    string TenantName,
    string TenantSlug,
    string PlanType,
    decimal MonthlyPrice,
    string BillingStatus,
    DateTime? TrialEndsAt,
    DateTime? NextBillingAt);

public record GetTenantBillingQuery(int? TenantId = null) : IRequest<Result<List<TenantBillingDto>>>;

public class GetTenantBillingQueryHandler : IRequestHandler<GetTenantBillingQuery, Result<List<TenantBillingDto>>>
{
    private readonly IShopDbContext _db;

    public GetTenantBillingQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<TenantBillingDto>>> Handle(GetTenantBillingQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tenants.AsNoTracking().AsQueryable();

        if (request.TenantId.HasValue)
            query = query.Where(t => t.Id == request.TenantId.Value);

        var results = await query
            .GroupJoin(
                _db.TenantPlans.AsNoTracking(),
                t => t.Id,
                p => p.TenantId,
                (t, plans) => new { Tenant = t, Plans = plans })
            .SelectMany(
                x => x.Plans.DefaultIfEmpty(),
                (x, plan) => new TenantBillingDto(
                    x.Tenant.Id,
                    x.Tenant.Name,
                    x.Tenant.Slug,
                    plan != null ? plan.PlanType : "Free",
                    plan != null ? plan.MonthlyPrice : 0,
                    plan != null ? plan.BillingStatus : "None",
                    plan != null ? plan.TrialEndsAt : null,
                    plan != null ? plan.NextBillingAt : null))
            .OrderBy(x => x.TenantId)
            .ToListAsync(cancellationToken);

        return Result<List<TenantBillingDto>>.Success(results);
    }
}
