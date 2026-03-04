using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Queries;

// ── DTOs ──

public record CommissionSettingDto(
    int Id,
    int TenantId,
    int? ProductId,
    int? CategoryId,
    decimal CommissionRate,
    string SettlementCycle,
    int SettlementDayOfWeek,
    decimal MinSettlementAmount,
    string? BankName,
    string? BankAccount,
    string? BankHolder);

public record CommissionDto(
    int Id,
    int TenantId,
    int OrderId,
    decimal OrderAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal SettlementAmount,
    string Status,
    int? SettlementId,
    DateTime CreatedAt);

public record SettlementDto(
    int Id,
    int TenantId,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int OrderCount,
    decimal TotalOrderAmount,
    decimal TotalCommission,
    decimal TotalSettlementAmount,
    string Status,
    string? BankName,
    string? BankAccount,
    string? TransactionId,
    DateTime? SettledAt,
    string? SettledBy,
    DateTime CreatedAt);

public record CommissionSummaryDto(
    int TenantId,
    string TenantName,
    int TotalOrders,
    decimal TotalOrderAmount,
    decimal TotalCommission,
    decimal TotalSettlementAmount,
    decimal PendingSettlementAmount);

// ── Get Commission Settings ──

public record GetCommissionSettingsQuery(int TenantId) : IRequest<Result<List<CommissionSettingDto>>>;

public class GetCommissionSettingsQueryHandler : IRequestHandler<GetCommissionSettingsQuery, Result<List<CommissionSettingDto>>>
{
    private readonly IShopDbContext _db;

    public GetCommissionSettingsQueryHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<CommissionSettingDto>>> Handle(GetCommissionSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _db.CommissionSettings
            .AsNoTracking()
            .Where(cs => cs.TenantId == request.TenantId)
            .OrderBy(cs => cs.ProductId).ThenBy(cs => cs.CategoryId)
            .Select(cs => new CommissionSettingDto(
                cs.Id, cs.TenantId, cs.ProductId, cs.CategoryId,
                cs.CommissionRate, cs.SettlementCycle, cs.SettlementDayOfWeek,
                cs.MinSettlementAmount, cs.BankName, cs.BankAccount, cs.BankHolder))
            .ToListAsync(cancellationToken);

        return Result<List<CommissionSettingDto>>.Success(settings);
    }
}

// ── Get Commissions ──

public record GetCommissionsQuery(int? TenantId = null, string? Status = null) : IRequest<Result<List<CommissionDto>>>;

public class GetCommissionsQueryHandler : IRequestHandler<GetCommissionsQuery, Result<List<CommissionDto>>>
{
    private readonly IShopDbContext _db;

    public GetCommissionsQueryHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<CommissionDto>>> Handle(GetCommissionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Commissions.AsNoTracking().AsQueryable();

        if (request.TenantId.HasValue)
            query = query.Where(c => c.TenantId == request.TenantId.Value);

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

// ── Get Settlements ──

public record GetSettlementsQuery(int? TenantId = null, string? Status = null) : IRequest<Result<List<SettlementDto>>>;

public class GetSettlementsQueryHandler : IRequestHandler<GetSettlementsQuery, Result<List<SettlementDto>>>
{
    private readonly IShopDbContext _db;

    public GetSettlementsQueryHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<SettlementDto>>> Handle(GetSettlementsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Settlements.AsNoTracking().AsQueryable();

        if (request.TenantId.HasValue)
            query = query.Where(s => s.TenantId == request.TenantId.Value);

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

// ── Get Commission Summary ──

public record GetCommissionSummaryQuery(int? TenantId = null) : IRequest<Result<List<CommissionSummaryDto>>>;

public class GetCommissionSummaryQueryHandler : IRequestHandler<GetCommissionSummaryQuery, Result<List<CommissionSummaryDto>>>
{
    private readonly IShopDbContext _db;

    public GetCommissionSummaryQueryHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<CommissionSummaryDto>>> Handle(GetCommissionSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Commissions.AsNoTracking().AsQueryable();

        if (request.TenantId.HasValue)
            query = query.Where(c => c.TenantId == request.TenantId.Value);

        var summaries = await query
            .GroupBy(c => c.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                TotalOrders = g.Count(),
                TotalOrderAmount = g.Sum(c => c.OrderAmount),
                TotalCommission = g.Sum(c => c.CommissionAmount),
                TotalSettlementAmount = g.Sum(c => c.SettlementAmount),
                PendingSettlementAmount = g.Where(c => c.Status == "Pending").Sum(c => c.SettlementAmount)
            })
            .ToListAsync(cancellationToken);

        // Join with tenant names
        var tenantIds = summaries.Select(s => s.TenantId).ToList();
        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var result = summaries.Select(s => new CommissionSummaryDto(
            s.TenantId,
            tenants.GetValueOrDefault(s.TenantId, "Unknown"),
            s.TotalOrders,
            s.TotalOrderAmount,
            s.TotalCommission,
            s.TotalSettlementAmount,
            s.PendingSettlementAmount
        )).ToList();

        return Result<List<CommissionSummaryDto>>.Success(result);
    }
}
