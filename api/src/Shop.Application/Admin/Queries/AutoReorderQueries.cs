using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

// ── Get Auto Reorder Rules ──

public record GetAutoReorderRulesQuery(bool? EnabledOnly = null) : IRequest<Result<List<AutoReorderRuleDto>>>;

public record AutoReorderRuleDto(
    int Id,
    int ProductId,
    string ProductName,
    int ReorderThreshold,
    int ReorderQuantity,
    int MaxStockLevel,
    bool IsEnabled,
    bool AutoForwardToMes,
    int MinIntervalHours,
    DateTime? LastTriggeredAt,
    int CurrentStock,
    DateTime CreatedAt);

public class GetAutoReorderRulesHandler : IRequestHandler<GetAutoReorderRulesQuery, Result<List<AutoReorderRuleDto>>>
{
    private readonly IShopDbContext _db;

    public GetAutoReorderRulesHandler(IShopDbContext db) => _db = db;

    public async Task<Result<List<AutoReorderRuleDto>>> Handle(GetAutoReorderRulesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AutoReorderRules.AsNoTracking().AsQueryable();

        if (request.EnabledOnly == true)
            query = query.Where(r => r.IsEnabled);

        var rules = await query
            .OrderBy(r => r.ProductName)
            .ToListAsync(cancellationToken);

        var productIds = rules.Select(r => r.ProductId).ToList();
        var stockMap = await _db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(v => v.Stock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, cancellationToken);

        var result = rules.Select(r => new AutoReorderRuleDto(
            r.Id, r.ProductId, r.ProductName,
            r.ReorderThreshold, r.ReorderQuantity, r.MaxStockLevel,
            r.IsEnabled, r.AutoForwardToMes, r.MinIntervalHours,
            r.LastTriggeredAt,
            stockMap.GetValueOrDefault(r.ProductId, 0),
            r.CreatedAt
        )).ToList();

        return Result<List<AutoReorderRuleDto>>.Success(result);
    }
}

// ── Get Purchase Orders ──

public record GetPurchaseOrdersQuery(string? Status = null, int Page = 1, int PageSize = 20) : IRequest<Result<PurchaseOrderListDto>>;

public record PurchaseOrderListDto(List<PurchaseOrderDto> Items, int Total, int Page, int PageSize);

public record PurchaseOrderDto(
    int Id,
    string OrderNumber,
    string Status,
    string TriggerType,
    int TotalQuantity,
    int ItemCount,
    string? MesOrderId,
    string? MesOrderNo,
    DateTime? ForwardedAt,
    DateTime? ConfirmedAt,
    string? Notes,
    string? CreatedByUser,
    DateTime CreatedAt,
    List<PurchaseOrderItemDto> Items);

public record PurchaseOrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? MesProductCode,
    int CurrentStock,
    int ReorderThreshold,
    int OrderedQuantity,
    int ReceivedQuantity,
    string? Reason);

public class GetPurchaseOrdersHandler : IRequestHandler<GetPurchaseOrdersQuery, Result<PurchaseOrderListDto>>
{
    private readonly IShopDbContext _db;

    public GetPurchaseOrdersHandler(IShopDbContext db) => _db = db;

    public async Task<Result<PurchaseOrderListDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PurchaseOrders.AsNoTracking()
            .Include(po => po.Items)
            .OrderByDescending(po => po.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(po => po.Status == request.Status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(po => new PurchaseOrderDto(
            po.Id, po.OrderNumber, po.Status, po.TriggerType,
            po.TotalQuantity, po.ItemCount,
            po.MesOrderId, po.MesOrderNo,
            po.ForwardedAt, po.ConfirmedAt,
            po.Notes, po.CreatedByUser, po.CreatedAt,
            po.Items.Select(i => new PurchaseOrderItemDto(
                i.Id, i.ProductId, i.ProductName, i.MesProductCode,
                i.CurrentStock, i.ReorderThreshold,
                i.OrderedQuantity, i.ReceivedQuantity, i.Reason
            )).ToList()
        )).ToList();

        return Result<PurchaseOrderListDto>.Success(
            new PurchaseOrderListDto(dtos, total, request.Page, request.PageSize));
    }
}

// ── Get Auto Reorder Dashboard Stats ──

public record GetAutoReorderStatsQuery() : IRequest<Result<AutoReorderStatsDto>>;

public record AutoReorderStatsDto(
    int TotalRules,
    int EnabledRules,
    int ProductsBelowThreshold,
    int TotalPurchaseOrders,
    int PendingOrders,
    int ForwardedOrders,
    DateTime? LastAutoRun);

public class GetAutoReorderStatsHandler : IRequestHandler<GetAutoReorderStatsQuery, Result<AutoReorderStatsDto>>
{
    private readonly IShopDbContext _db;

    public GetAutoReorderStatsHandler(IShopDbContext db) => _db = db;

    public async Task<Result<AutoReorderStatsDto>> Handle(GetAutoReorderStatsQuery request, CancellationToken cancellationToken)
    {
        var totalRules = await _db.AutoReorderRules.CountAsync(cancellationToken);
        var enabledRules = await _db.AutoReorderRules.CountAsync(r => r.IsEnabled, cancellationToken);

        // Check products below threshold
        var enabledRulesData = await _db.AutoReorderRules.AsNoTracking()
            .Where(r => r.IsEnabled)
            .Select(r => new { r.ProductId, r.ReorderThreshold })
            .ToListAsync(cancellationToken);

        var productIds = enabledRulesData.Select(r => r.ProductId).ToList();
        var stockMap = await _db.ProductVariants.AsNoTracking()
            .Where(v => productIds.Contains(v.ProductId) && v.IsActive)
            .GroupBy(v => v.ProductId)
            .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(v => v.Stock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.TotalStock, cancellationToken);

        var belowThreshold = enabledRulesData
            .Count(r => stockMap.GetValueOrDefault(r.ProductId, 0) <= r.ReorderThreshold);

        var totalPOs = await _db.PurchaseOrders.CountAsync(cancellationToken);
        var pendingPOs = await _db.PurchaseOrders.CountAsync(po => po.Status == "Created", cancellationToken);
        var forwardedPOs = await _db.PurchaseOrders.CountAsync(po => po.Status == "Forwarded", cancellationToken);

        var lastAutoRun = await _db.PurchaseOrders.AsNoTracking()
            .Where(po => po.TriggerType == "Auto")
            .OrderByDescending(po => po.CreatedAt)
            .Select(po => (DateTime?)po.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<AutoReorderStatsDto>.Success(new AutoReorderStatsDto(
            totalRules, enabledRules, belowThreshold,
            totalPOs, pendingPOs, forwardedPOs, lastAutoRun));
    }
}
