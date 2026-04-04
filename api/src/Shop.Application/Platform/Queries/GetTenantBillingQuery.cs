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

        var tenants = await query.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        var plans = await _db.TenantPlans.AsNoTracking().ToListAsync(cancellationToken);
        var planMap = plans.ToDictionary(p => p.TenantId);

        var results = tenants.Select(t =>
        {
            planMap.TryGetValue(t.Id, out var plan);
            return new TenantBillingDto(
                t.Id,
                t.Name,
                t.Slug,
                plan?.PlanType ?? "Free",
                plan?.MonthlyPrice ?? 0,
                plan?.BillingStatus ?? "None",
                plan?.TrialEndsAt,
                plan?.NextBillingAt);
        }).ToList();

        return Result<List<TenantBillingDto>>.Success(results);
    }
}
