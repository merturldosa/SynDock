using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Queries;

public record GetOrdersQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedOrderResult>;

public record PagedOrderResult(
    IReadOnlyList<OrderSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedOrderResult>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrdersQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedOrderResult> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return new PagedOrderResult(new List<OrderSummaryDto>(), 0, request.Page, request.PageSize);

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Where(o => o.UserId == _currentUser.UserId.Value);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(o => o.Status == request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o =>
        {
            var firstItem = o.Items.FirstOrDefault();
            return new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Items.Count,
                o.TotalAmount,
                firstItem?.ProductName,
                firstItem?.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                o.CreatedAt);
        }).ToList();

        return new PagedOrderResult(items, totalCount, request.Page, request.PageSize);
    }
}
