using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Platform.Queries;

public record InvoiceDto(
    int Id,
    int TenantId,
    string TenantName,
    string InvoiceNumber,
    decimal Amount,
    string Status,
    string BillingPeriod,
    string? PlanType,
    DateTime IssuedAt,
    DateTime? PaidAt,
    string? TransactionId,
    string? PaymentMethod);

public record GetInvoicesQuery(string? Slug = null) : IRequest<Result<List<InvoiceDto>>>;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<List<InvoiceDto>>>
{
    private readonly IShopDbContext _db;

    public GetInvoicesQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Invoices.AsNoTracking()
            .Join(_db.Tenants.AsNoTracking(),
                i => i.TenantId,
                t => t.Id,
                (i, t) => new { Invoice = i, Tenant = t });

        if (!string.IsNullOrEmpty(request.Slug))
            query = query.Where(x => x.Tenant.Slug == request.Slug);

        var results = await query
            .OrderByDescending(x => x.Invoice.IssuedAt)
            .Select(x => new InvoiceDto(
                x.Invoice.Id,
                x.Invoice.TenantId,
                x.Tenant.Name,
                x.Invoice.InvoiceNumber,
                x.Invoice.Amount,
                x.Invoice.Status,
                x.Invoice.BillingPeriod,
                x.Invoice.PlanType,
                x.Invoice.IssuedAt,
                x.Invoice.PaidAt,
                x.Invoice.TransactionId,
                x.Invoice.PaymentMethod))
            .ToListAsync(cancellationToken);

        return Result<List<InvoiceDto>>.Success(results);
    }
}
