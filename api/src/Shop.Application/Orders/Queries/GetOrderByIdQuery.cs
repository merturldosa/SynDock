using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Queries;

public record GetOrderByIdQuery(int OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return null;

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Variant)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Histories)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == _currentUser.UserId.Value, cancellationToken);

        if (order is null)
            return null;

        var items = order.Items.Select(oi => new OrderItemDto(
            oi.Id,
            oi.ProductId,
            oi.ProductName,
            oi.Product?.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
            oi.VariantId,
            oi.Variant?.Name,
            oi.Quantity,
            oi.UnitPrice,
            oi.TotalPrice)).ToList();

        AddressDto? addressDto = null;
        if (order.ShippingAddress is not null)
        {
            var a = order.ShippingAddress;
            addressDto = new AddressDto(a.Id, a.RecipientName, a.Phone, a.ZipCode, a.Address1, a.Address2, a.IsDefault);
        }

        var histories = order.Histories
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new OrderHistoryDto(h.Id, h.Status, h.Note, h.TrackingNumber, h.TrackingCarrier, h.CreatedBy, h.CreatedAt))
            .ToList();

        // Get latest tracking info
        var latestShipping = order.Histories
            .Where(h => h.TrackingNumber != null)
            .OrderByDescending(h => h.CreatedAt)
            .FirstOrDefault();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            items,
            order.TotalAmount,
            order.ShippingFee,
            order.DiscountAmount,
            order.PointsUsed,
            order.CouponId,
            order.Note,
            addressDto,
            order.CreatedAt,
            histories,
            latestShipping?.TrackingNumber,
            latestShipping?.TrackingCarrier);
    }
}
