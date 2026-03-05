using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record UpdateTenantPlanCommand(int TenantId, string PlanType, decimal MonthlyPrice) : IRequest<Result<bool>>;

public class UpdateTenantPlanCommandHandler : IRequestHandler<UpdateTenantPlanCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public UpdateTenantPlanCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(UpdateTenantPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId, cancellationToken);

        if (plan is null)
        {
            plan = new TenantPlan
            {
                TenantId = request.TenantId,
                PlanType = request.PlanType,
                MonthlyPrice = request.MonthlyPrice,
                BillingStatus = "Active",
                NextBillingAt = DateTime.UtcNow.AddMonths(1),
                CreatedBy = "PlatformAdmin"
            };
            await _db.TenantPlans.AddAsync(plan, cancellationToken);
        }
        else
        {
            plan.PlanType = request.PlanType;
            plan.MonthlyPrice = request.MonthlyPrice;
            plan.UpdatedBy = "PlatformAdmin";
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public record UpdateBillingStatusCommand(int TenantId, string BillingStatus) : IRequest<Result<bool>>;

public class UpdateBillingStatusCommandHandler : IRequestHandler<UpdateBillingStatusCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public UpdateBillingStatusCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(UpdateBillingStatusCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId, cancellationToken);

        if (plan is null)
            return Result<bool>.Failure("Billing information not found.");

        plan.BillingStatus = request.BillingStatus;
        plan.UpdatedBy = "PlatformAdmin";
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
