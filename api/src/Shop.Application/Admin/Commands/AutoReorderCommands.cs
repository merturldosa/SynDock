using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Commands;

// ── Upsert Auto Reorder Rule ──

public record UpsertAutoReorderRuleCommand(
    int ProductId,
    int ReorderThreshold,
    int ReorderQuantity,
    int MaxStockLevel,
    bool IsEnabled,
    bool AutoForwardToMes,
    int MinIntervalHours) : IRequest<Result<int>>;

public class UpsertAutoReorderRuleHandler : IRequestHandler<UpsertAutoReorderRuleCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public UpsertAutoReorderRuleHandler(IShopDbContext db) => _db = db;

    public async Task<Result<int>> Handle(UpsertAutoReorderRuleCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return Result<int>.Failure("Product not found.");

        var rule = await _db.AutoReorderRules
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId, cancellationToken);

        if (rule is null)
        {
            rule = new AutoReorderRule
            {
                ProductId = request.ProductId,
                ProductName = product.Name,
                ReorderThreshold = request.ReorderThreshold,
                ReorderQuantity = request.ReorderQuantity,
                MaxStockLevel = request.MaxStockLevel,
                IsEnabled = request.IsEnabled,
                AutoForwardToMes = request.AutoForwardToMes,
                MinIntervalHours = request.MinIntervalHours
            };
            _db.AutoReorderRules.Add(rule);
        }
        else
        {
            rule.ProductName = product.Name;
            rule.ReorderThreshold = request.ReorderThreshold;
            rule.ReorderQuantity = request.ReorderQuantity;
            rule.MaxStockLevel = request.MaxStockLevel;
            rule.IsEnabled = request.IsEnabled;
            rule.AutoForwardToMes = request.AutoForwardToMes;
            rule.MinIntervalHours = request.MinIntervalHours;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(rule.Id);
    }
}

// ── Delete Auto Reorder Rule ──

public record DeleteAutoReorderRuleCommand(int Id) : IRequest<Result<bool>>;

public class DeleteAutoReorderRuleHandler : IRequestHandler<DeleteAutoReorderRuleCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public DeleteAutoReorderRuleHandler(IShopDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteAutoReorderRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _db.AutoReorderRules.FindAsync(new object[] { request.Id }, cancellationToken);
        if (rule is null)
            return Result<bool>.Failure("Rule not found.");

        _db.AutoReorderRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

// ── Toggle Auto Reorder Rule ──

public record ToggleAutoReorderRuleCommand(int Id, bool IsEnabled) : IRequest<Result<bool>>;

public class ToggleAutoReorderRuleHandler : IRequestHandler<ToggleAutoReorderRuleCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public ToggleAutoReorderRuleHandler(IShopDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(ToggleAutoReorderRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _db.AutoReorderRules.FindAsync(new object[] { request.Id }, cancellationToken);
        if (rule is null)
            return Result<bool>.Failure("Rule not found.");

        rule.IsEnabled = request.IsEnabled;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

// ── Bulk Create Auto Reorder Rules ──

public record BulkCreateAutoReorderRulesCommand(int ReorderThreshold, int MinIntervalHours, bool AutoForwardToMes) : IRequest<Result<int>>;

public class BulkCreateAutoReorderRulesHandler : IRequestHandler<BulkCreateAutoReorderRulesCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public BulkCreateAutoReorderRulesHandler(IShopDbContext db) => _db = db;

    public async Task<Result<int>> Handle(BulkCreateAutoReorderRulesCommand request, CancellationToken cancellationToken)
    {
        var existingProductIds = await _db.AutoReorderRules.AsNoTracking()
            .Select(r => r.ProductId)
            .ToListAsync(cancellationToken);

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive && !existingProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var product in products)
        {
            _db.AutoReorderRules.Add(new AutoReorderRule
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ReorderThreshold = request.ReorderThreshold,
                ReorderQuantity = 0, // auto-calculate from forecast
                IsEnabled = true,
                AutoForwardToMes = request.AutoForwardToMes,
                MinIntervalHours = request.MinIntervalHours
            });
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(count);
    }
}

// ── Cancel Purchase Order ──

public record CancelPurchaseOrderCommand(int Id) : IRequest<Result<bool>>;

public class CancelPurchaseOrderHandler : IRequestHandler<CancelPurchaseOrderCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public CancelPurchaseOrderHandler(IShopDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var po = await _db.PurchaseOrders.FindAsync(new object[] { request.Id }, cancellationToken);
        if (po is null)
            return Result<bool>.Failure("Purchase order not found.");
        if (po.Status is "Received" or "Cancelled")
            return Result<bool>.Failure("Purchase order is already completed or cancelled.");

        po.Status = "Cancelled";
        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
