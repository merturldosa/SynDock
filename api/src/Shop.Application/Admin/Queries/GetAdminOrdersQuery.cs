using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Admin.Queries;

public record AdminOrderSummaryDto(
    int Id, string OrderNumber, string Status, int ItemCount, decimal TotalAmount,
    string? FirstProductName, string? FirstProductImageUrl,
    string? CustomerName, string? CustomerEmail, DateTime CreatedAt);

public record AdminPagedOrderResult(
    IReadOnlyList<AdminOrderSummaryDto> Items, int TotalCount, int Page, int PageSize);

public record GetAdminOrdersQuery(
    string? Status, string? Search, int Page = 1, int PageSize = 20
) : IRequest<Result<AdminPagedOrderResult>>;

public class GetAdminOrdersQueryHandler : IRequestHandler<GetAdminOrdersQuery, Result<AdminPagedOrderResult>>
{
    private readonly IShopDbContext _db;

    public GetAdminOrdersQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AdminPagedOrderResult>> Handle(GetAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(o => o.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(search) ||
                o.User.Name.ToLower().Contains(search) ||
                o.User.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o =>
        {
            var firstItem = o.Items.OrderBy(i => i.Id).FirstOrDefault();
            return new AdminOrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Items.Count,
                o.TotalAmount,
                firstItem?.ProductName,
                firstItem?.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                o.User.Name,
                o.User.Email,
                o.CreatedAt);
        }).ToList();

        return Result<AdminPagedOrderResult>.Success(
            new AdminPagedOrderResult(items, totalCount, request.Page, request.PageSize));
    }
}
