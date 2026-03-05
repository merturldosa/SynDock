using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Common;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Queries;

public record TenantUsageDto(
    int TenantId,
    string TenantName,
    string TenantSlug,
    string PlanType,
    int ProductCount,
    int MaxProducts,
    int UserCount,
    int MaxUsers,
    int MonthlyOrderCount,
    int MaxMonthlyOrders,
    long StorageUsedBytes,
    long MaxStorageBytes,
    string CurrentPeriod);

public record GetTenantUsageQuery(string Slug) : IRequest<Result<TenantUsageDto>>;

public class GetTenantUsageQueryHandler : IRequestHandler<GetTenantUsageQuery, Result<TenantUsageDto>>
{
    private readonly IShopDbContext _db;
    private readonly IPlanEnforcer _planEnforcer;

    public GetTenantUsageQueryHandler(IShopDbContext db, IPlanEnforcer planEnforcer)
    {
        _db = db;
        _planEnforcer = planEnforcer;
    }

    public async Task<Result<TenantUsageDto>> Handle(GetTenantUsageQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == request.Slug, cancellationToken);

        if (tenant is null)
            return Result<TenantUsageDto>.Failure("Tenant not found.");

        await _planEnforcer.EnsureUsageTracked(tenant.Id, cancellationToken);

        var usage = await _db.TenantUsages.AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id, cancellationToken);

        var plan = await _db.TenantPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id, cancellationToken);

        var limits = PlanLimits.GetLimits(plan?.PlanType ?? "Free");

        var dto = new TenantUsageDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            plan?.PlanType ?? "Free",
            usage?.ProductCount ?? 0,
            limits.MaxProducts,
            usage?.UserCount ?? 0,
            limits.MaxUsers,
            usage?.MonthlyOrderCount ?? 0,
            limits.MaxMonthlyOrders,
            usage?.StorageUsedBytes ?? 0,
            limits.MaxStorageBytes,
            usage?.CurrentPeriod ?? DateTime.UtcNow.ToString("yyyy-MM"));

        return Result<TenantUsageDto>.Success(dto);
    }
}
