using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record MyTenantBillingDto(
    string TenantName,
    string TenantSlug,
    string PlanType,
    decimal MonthlyPrice,
    string BillingStatus,
    DateTime? TrialEndsAt,
    DateTime? NextBillingAt,
    int TotalProducts,
    int TotalOrders,
    int TotalUsers);

public record GetMyTenantBillingQuery : IRequest<Result<MyTenantBillingDto>>;

public class GetMyTenantBillingQueryHandler : IRequestHandler<GetMyTenantBillingQuery, Result<MyTenantBillingDto>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMyTenantBillingQueryHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<MyTenantBillingDto>> Handle(GetMyTenantBillingQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<MyTenantBillingDto>.Failure("Tenant information not found.");

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return Result<MyTenantBillingDto>.Failure("Tenant not found.");

        var plan = await _db.TenantPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);

        var totalProducts = await _db.Products.CountAsync(cancellationToken);
        var totalOrders = await _db.Orders.CountAsync(cancellationToken);
        var totalUsers = await _db.Users.CountAsync(cancellationToken);

        var dto = new MyTenantBillingDto(
            tenant.Name,
            tenant.Slug,
            plan?.PlanType ?? "Free",
            plan?.MonthlyPrice ?? 0,
            plan?.BillingStatus ?? "None",
            plan?.TrialEndsAt,
            plan?.NextBillingAt,
            totalProducts,
            totalOrders,
            totalUsers);

        return Result<MyTenantBillingDto>.Success(dto);
    }
}
