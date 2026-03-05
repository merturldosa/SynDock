using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Commands;

public record GenerateInvoiceCommand(int TenantId, string BillingPeriod) : IRequest<Result<int>>;

public class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public GenerateInvoiceCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Check if invoice already exists for this period
        var exists = await _db.Invoices
            .AnyAsync(i => i.TenantId == request.TenantId && i.BillingPeriod == request.BillingPeriod, cancellationToken);

        if (exists)
            return Result<int>.Failure($"Invoice already exists for period {request.BillingPeriod}.");

        var plan = await _db.TenantPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId, cancellationToken);

        if (plan is null || plan.MonthlyPrice <= 0)
            return Result<int>.Failure("No paid plan configured.");

        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        var invoice = new Invoice
        {
            TenantId = request.TenantId,
            InvoiceNumber = invoiceNumber,
            Amount = plan.MonthlyPrice,
            Status = "Pending",
            BillingPeriod = request.BillingPeriod,
            PlanType = plan.PlanType,
            IssuedAt = DateTime.UtcNow,
            CreatedBy = "BillingSystem"
        };

        await _db.Invoices.AddAsync(invoice, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(invoice.Id);
    }
}

public record MarkInvoicePaidCommand(int InvoiceId, string? TransactionId, string? PaymentMethod) : IRequest<Result<bool>>;

public class MarkInvoicePaidCommandHandler : IRequestHandler<MarkInvoicePaidCommand, Result<bool>>
{
    private readonly IShopDbContext _db;

    public MarkInvoicePaidCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<bool>> Handle(MarkInvoicePaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _db.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            return Result<bool>.Failure("Invoice not found.");

        if (invoice.Status == "Paid")
            return Result<bool>.Failure("Invoice has already been paid.");

        invoice.Status = "Paid";
        invoice.PaidAt = DateTime.UtcNow;
        invoice.TransactionId = request.TransactionId;
        invoice.PaymentMethod = request.PaymentMethod;
        invoice.UpdatedBy = "PlatformAdmin";
        invoice.UpdatedAt = DateTime.UtcNow;

        // Update billing status to Active and set next billing date
        var plan = await _db.TenantPlans
            .FirstOrDefaultAsync(p => p.TenantId == invoice.TenantId, cancellationToken);

        if (plan is not null)
        {
            plan.BillingStatus = "Active";
            plan.NextBillingAt = DateTime.UtcNow.AddMonths(1);
            plan.UpdatedBy = "BillingSystem";
            plan.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
