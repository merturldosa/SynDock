using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Carts.Queries;

public record GetCartQuery : IRequest<CartDto?>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return null;

        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Variant)
            .FirstOrDefaultAsync(c => c.UserId == _currentUser.UserId.Value, cancellationToken);

        if (cart is null)
            return new CartDto(0, new List<CartItemDto>(), 0, 0);

        var items = cart.Items.Select(ci =>
        {
            var effectivePrice = ci.Variant?.Price ?? ci.Product.SalePrice ?? ci.Product.Price;
            return new CartItemDto(
                ci.Id,
                ci.ProductId,
                ci.Product.Name,
                ci.Product.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                ci.Product.Price,
                ci.Product.SalePrice,
                ci.Product.PriceType,
                ci.VariantId,
                ci.Variant?.Name,
                ci.Quantity,
                effectivePrice * ci.Quantity);
        }).ToList();

        return new CartDto(
            cart.Id,
            items,
            items.Sum(i => i.SubTotal),
            items.Sum(i => i.Quantity));
    }
}
