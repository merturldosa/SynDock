using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services.Scm;

public class ScmService : IScmService
{
    private readonly IShopDbContext _db;
    private readonly IMediator _mediator;

    public ScmService(IShopDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    // === Suppliers ===

    public async Task<List<Supplier>> GetSuppliersAsync(int tenantId, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);
        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<Supplier?> GetSupplierAsync(int tenantId, int supplierId, CancellationToken ct = default)
        => await _db.Suppliers.AsNoTracking()
            .Include(s => s.ProcurementOrders.OrderByDescending(po => po.CreatedAt).Take(10))
            .FirstOrDefaultAsync(s => s.Id == supplierId, ct);

    public async Task<Supplier> CreateSupplierAsync(int tenantId, string name, string code, string? contactName, string? email, string? phone, string? address, string? businessNumber, int leadTimeDays, string createdBy, CancellationToken ct = default)
    {
        var supplier = new Supplier
        {
            TenantId = tenantId,
            Name = name,
            Code = code,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Address = address,
            BusinessNumber = businessNumber,
            LeadTimeDays = leadTimeDays,
            CreatedBy = createdBy
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);
        return supplier;
    }

    public async Task UpdateSupplierAsync(int tenantId, int supplierId, string? status, string? grade, string? notes, string updatedBy, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, ct)
            ?? throw new InvalidOperationException("Supplier not found");
        if (status is not null) supplier.Status = status;
        if (grade is not null) supplier.Grade = grade;
        if (notes is not null) supplier.Notes = notes;
        supplier.UpdatedBy = updatedBy;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // === Procurement Orders ===

    public async Task<ProcurementOrder> CreateProcurementOrderAsync(int tenantId, int supplierId, List<(int productId, string productName, int quantity, decimal unitPrice)> items, DateTime? expectedDelivery, string? notes, string createdBy, CancellationToken ct = default)
    {
        var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var order = new ProcurementOrder
        {
            TenantId = tenantId,
            OrderNumber = orderNumber,
            SupplierId = supplierId,
            Status = "Draft",
            ExpectedDeliveryDate = expectedDelivery,
            Notes = notes,
            CreatedBy = createdBy
        };

        foreach (var (productId, productName, quantity, unitPrice) in items)
        {
            order.Items.Add(new ProcurementOrderItem
            {
                TenantId = tenantId,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * quantity,
                CreatedBy = createdBy
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

        _db.ProcurementOrders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<ProcurementOrder?> GetProcurementOrderAsync(int tenantId, int orderId, CancellationToken ct = default)
        => await _db.ProcurementOrders.AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .FirstOrDefaultAsync(po => po.Id == orderId, ct);

    public async Task<(List<ProcurementOrder> Items, int TotalCount)> GetProcurementOrdersAsync(int tenantId, string? status = null, int? supplierId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = _db.ProcurementOrders.AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) query = query.Where(po => po.Status == status);
        if (supplierId.HasValue) query = query.Where(po => po.SupplierId == supplierId.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(po => po.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task SubmitProcurementOrderAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default)
    {
        var order = await _db.ProcurementOrders.FirstOrDefaultAsync(po => po.Id == orderId, ct)
            ?? throw new InvalidOperationException("Procurement order not found");
        if (order.Status != "Draft") throw new InvalidOperationException($"Cannot submit order in '{order.Status}' status");
        order.Status = "Submitted";
        order.SubmittedAt = DateTime.UtcNow;
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ConfirmProcurementOrderAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default)
    {
        var order = await _db.ProcurementOrders.FirstOrDefaultAsync(po => po.Id == orderId, ct)
            ?? throw new InvalidOperationException("Procurement order not found");
        if (order.Status != "Submitted") throw new InvalidOperationException($"Cannot confirm order in '{order.Status}' status");
        order.Status = "Confirmed";
        order.ConfirmedAt = DateTime.UtcNow;
        order.ApprovedByUserId = int.TryParse(updatedBy, out var uid) ? uid : null;
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkShippedAsync(int tenantId, int orderId, string? trackingNumber, string updatedBy, CancellationToken ct = default)
    {
        var order = await _db.ProcurementOrders.FirstOrDefaultAsync(po => po.Id == orderId, ct)
            ?? throw new InvalidOperationException("Procurement order not found");
        if (order.Status != "Confirmed") throw new InvalidOperationException($"Cannot mark shipped for order in '{order.Status}' status");
        order.Status = "Shipped";
        order.ShippedAt = DateTime.UtcNow;
        order.TrackingNumber = trackingNumber;
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkDeliveredAsync(int tenantId, int orderId, string updatedBy, CancellationToken ct = default)
    {
        var order = await _db.ProcurementOrders
            .Include(po => po.Items)
            .FirstOrDefaultAsync(po => po.Id == orderId, ct)
            ?? throw new InvalidOperationException("Procurement order not found");
        if (order.Status != "Shipped") throw new InvalidOperationException($"Cannot mark delivered for order in '{order.Status}' status");

        order.Status = "Delivered";
        order.DeliveredAt = DateTime.UtcNow;
        order.ActualDeliveryDate = DateTime.UtcNow;
        order.UpdatedBy = updatedBy;
        order.UpdatedAt = DateTime.UtcNow;

        // Mark all items as fully received
        foreach (var item in order.Items)
        {
            item.ReceivedQuantity = item.Quantity;
            item.UpdatedBy = updatedBy;
            item.UpdatedAt = DateTime.UtcNow;
        }

        // Update supplier stats
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == order.SupplierId, ct);
        if (supplier is not null)
        {
            supplier.TotalOrders++;
            supplier.TotalAmount += order.TotalAmount;

            // Calculate on-time delivery rate
            var deliveredOrders = await _db.ProcurementOrders
                .Where(po => po.SupplierId == supplier.Id && po.Status == "Delivered")
                .ToListAsync(ct);

            var onTimeCount = deliveredOrders.Count(po =>
                po.ExpectedDeliveryDate == null || po.ActualDeliveryDate <= po.ExpectedDeliveryDate);
            var totalDelivered = deliveredOrders.Count + 1; // +1 for current order
            var currentOnTime = (order.ExpectedDeliveryDate == null || order.ActualDeliveryDate <= order.ExpectedDeliveryDate) ? 1 : 0;

            supplier.OnTimeDeliveryRate = Math.Round((decimal)(onTimeCount + currentOnTime) / totalDelivered * 100, 2);
            supplier.UpdatedBy = updatedBy;
            supplier.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        // Publish event for WMS goods receipt auto-creation
        try { await _mediator.Publish(new ProcurementDeliveredEvent(order.Id, order.TenantId, order.OrderNumber), ct); }
        catch { /* Logged by handler */ }
    }

    // === Supplier Evaluation ===

    public async Task<SupplierEvaluation> EvaluateSupplierAsync(int tenantId, int supplierId, string period, int qualityScore, int deliveryScore, int priceScore, int serviceScore, string? comments, string createdBy, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId, ct)
            ?? throw new InvalidOperationException("Supplier not found");

        var totalScore = (qualityScore + deliveryScore + priceScore + serviceScore) / 4;
        var grade = totalScore >= 90 ? "S" : totalScore >= 80 ? "A" : totalScore >= 60 ? "B" : totalScore >= 40 ? "C" : "D";

        var evaluation = new SupplierEvaluation
        {
            TenantId = tenantId,
            SupplierId = supplierId,
            EvaluationPeriod = period,
            QualityScore = qualityScore,
            DeliveryScore = deliveryScore,
            PriceScore = priceScore,
            ServiceScore = serviceScore,
            TotalScore = totalScore,
            Grade = grade,
            Comments = comments,
            CreatedBy = createdBy
        };

        _db.SupplierEvaluations.Add(evaluation);

        // Update supplier grade based on latest evaluation
        supplier.Grade = grade;
        supplier.UpdatedBy = createdBy;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return evaluation;
    }

    public async Task<List<SupplierEvaluation>> GetEvaluationsAsync(int tenantId, int? supplierId = null, CancellationToken ct = default)
    {
        var query = _db.SupplierEvaluations.AsNoTracking().Include(e => e.Supplier).AsQueryable();
        if (supplierId.HasValue) query = query.Where(e => e.SupplierId == supplierId.Value);
        return await query.OrderByDescending(e => e.EvaluationPeriod).ToListAsync(ct);
    }

    // === Analytics ===

    public async Task<object> GetScmDashboardAsync(int tenantId, CancellationToken ct = default)
    {
        var activeSuppliers = await _db.Suppliers.CountAsync(s => s.Status == "Active", ct);
        var totalSuppliers = await _db.Suppliers.CountAsync(ct);

        var openPOs = await _db.ProcurementOrders
            .Where(po => po.Status != "Delivered" && po.Status != "Cancelled")
            .CountAsync(ct);

        var overduePOs = await _db.ProcurementOrders
            .Where(po => po.Status != "Delivered" && po.Status != "Cancelled"
                && po.ExpectedDeliveryDate != null && po.ExpectedDeliveryDate < DateTime.UtcNow)
            .CountAsync(ct);

        var totalProcurementAmount = await _db.ProcurementOrders
            .Where(po => po.Status == "Delivered")
            .SumAsync(po => (decimal?)po.TotalAmount ?? 0, ct);

        var thisMonthAmount = await _db.ProcurementOrders
            .Where(po => po.Status == "Delivered"
                && po.DeliveredAt != null
                && po.DeliveredAt.Value.Year == DateTime.UtcNow.Year
                && po.DeliveredAt.Value.Month == DateTime.UtcNow.Month)
            .SumAsync(po => (decimal?)po.TotalAmount ?? 0, ct);

        var topSuppliers = await _db.Suppliers
            .Where(s => s.Status == "Active")
            .OrderByDescending(s => s.TotalAmount)
            .Take(5)
            .Select(s => new { s.Id, s.Name, s.Code, s.Grade, s.TotalOrders, s.TotalAmount, s.OnTimeDeliveryRate })
            .ToListAsync(ct);

        var recentOrders = await _db.ProcurementOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .OrderByDescending(po => po.CreatedAt)
            .Take(10)
            .Select(po => new { po.Id, po.OrderNumber, po.Status, po.TotalAmount, SupplierName = po.Supplier.Name, po.CreatedAt, po.ExpectedDeliveryDate })
            .ToListAsync(ct);

        var statusBreakdown = await _db.ProcurementOrders
            .GroupBy(po => po.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new
        {
            activeSuppliers,
            totalSuppliers,
            openPOs,
            overduePOs,
            totalProcurementAmount,
            thisMonthAmount,
            topSuppliers,
            recentOrders,
            statusBreakdown
        };
    }

    public async Task<object> GetLeadTimeAnalysisAsync(int tenantId, CancellationToken ct = default)
    {
        var deliveredOrders = await _db.ProcurementOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Where(po => po.Status == "Delivered" && po.SubmittedAt != null && po.DeliveredAt != null)
            .ToListAsync(ct);

        var bySupplier = deliveredOrders
            .GroupBy(po => new { po.SupplierId, po.Supplier.Name, po.Supplier.Code })
            .Select(g =>
            {
                var leadTimes = g.Select(po => (po.DeliveredAt!.Value - po.SubmittedAt!.Value).TotalDays).ToList();
                var onTimeCount = g.Count(po => po.ExpectedDeliveryDate == null || po.ActualDeliveryDate <= po.ExpectedDeliveryDate);
                return new
                {
                    supplierId = g.Key.SupplierId,
                    supplierName = g.Key.Name,
                    supplierCode = g.Key.Code,
                    orderCount = g.Count(),
                    avgLeadTimeDays = Math.Round(leadTimes.Average(), 1),
                    minLeadTimeDays = Math.Round(leadTimes.Min(), 1),
                    maxLeadTimeDays = Math.Round(leadTimes.Max(), 1),
                    onTimeDeliveryRate = Math.Round((double)onTimeCount / g.Count() * 100, 1)
                };
            })
            .OrderBy(x => x.avgLeadTimeDays)
            .ToList();

        var overall = deliveredOrders.Count > 0
            ? new
            {
                totalDelivered = deliveredOrders.Count,
                avgLeadTimeDays = Math.Round(deliveredOrders.Average(po => (po.DeliveredAt!.Value - po.SubmittedAt!.Value).TotalDays), 1),
                onTimeRate = Math.Round((double)deliveredOrders.Count(po => po.ExpectedDeliveryDate == null || po.ActualDeliveryDate <= po.ExpectedDeliveryDate) / deliveredOrders.Count * 100, 1)
            }
            : null;

        return new { overall, bySupplier };
    }
}
