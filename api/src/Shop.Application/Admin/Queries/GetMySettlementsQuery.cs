using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Platform.Queries;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record GetMySettlementsQuery(string? Status = null) : IRequest<Result<List<SettlementDto>>>;

public class GetMySettlementsQueryHandler : IRequestHandler<GetMySettlementsQuery, Result<List<SettlementDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMySettlementsQueryHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<SettlementDto>>> Handle(GetMySettlementsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<List<SettlementDto>>.Failure("Tenant information not found.");

        var query = _db.Settlements.AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(s => s.Status == request.Status);

        var results = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .Select(s => new SettlementDto(
                s.Id, s.TenantId, s.PeriodStart, s.PeriodEnd,
                s.OrderCount, s.TotalOrderAmount, s.TotalCommission, s.TotalSettlementAmount,
                s.Status, s.BankName, s.BankAccount, s.TransactionId,
                s.SettledAt, s.SettledBy, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<SettlementDto>>.Success(results);
    }
}

public record GetMyCommissionsQuery(string? Status = null) : IRequest<Result<List<CommissionDto>>>;

public class GetMyCommissionsQueryHandler : IRequestHandler<GetMyCommissionsQuery, Result<List<CommissionDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMyCommissionsQueryHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<CommissionDto>>> Handle(GetMyCommissionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<List<CommissionDto>>.Failure("Tenant information not found.");

        var query = _db.Commissions.AsNoTracking()
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.Status == request.Status);

        var results = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(200)
            .Select(c => new CommissionDto(
                c.Id, c.TenantId, c.OrderId,
                c.OrderAmount, c.CommissionRate, c.CommissionAmount, c.SettlementAmount,
                c.Status, c.SettlementId, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<CommissionDto>>.Success(results);
    }
}

public record GetMyCommissionSettingsQuery : IRequest<Result<List<CommissionSettingDto>>>;

public class GetMyCommissionSettingsQueryHandler : IRequestHandler<GetMyCommissionSettingsQuery, Result<List<CommissionSettingDto>>>
{
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetMyCommissionSettingsQueryHandler(IShopDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<CommissionSettingDto>>> Handle(GetMyCommissionSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == 0)
            return Result<List<CommissionSettingDto>>.Failure("Tenant information not found.");

        var settings = await _db.CommissionSettings
            .AsNoTracking()
            .Where(cs => cs.TenantId == tenantId)
            .OrderBy(cs => cs.ProductId).ThenBy(cs => cs.CategoryId)
            .Select(cs => new CommissionSettingDto(
                cs.Id, cs.TenantId, cs.ProductId, cs.CategoryId,
                cs.CommissionRate, cs.SettlementCycle, cs.SettlementDayOfWeek,
                cs.MinSettlementAmount, cs.BankName, cs.BankAccount, cs.BankHolder))
            .ToListAsync(cancellationToken);

        return Result<List<CommissionSettingDto>>.Success(settings);
    }
}
