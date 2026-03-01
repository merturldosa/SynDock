using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Orders.Queries;

public record GetOrderHistoryQuery(int OrderId) : IRequest<IReadOnlyList<OrderHistoryDto>>;

public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, IReadOnlyList<OrderHistoryDto>>
{
    private readonly IShopDbContext _db;

    public GetOrderHistoryQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrderHistoryDto>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _db.OrderHistories
            .AsNoTracking()
            .Where(h => h.OrderId == request.OrderId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new OrderHistoryDto(
                h.Id,
                h.Status,
                h.Note,
                h.TrackingNumber,
                h.TrackingCarrier,
                h.CreatedBy,
                h.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
