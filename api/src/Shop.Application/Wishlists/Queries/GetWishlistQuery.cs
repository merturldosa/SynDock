using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Wishlists.Queries;

public record GetWishlistQuery : IRequest<IReadOnlyList<WishlistItemDto>>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, IReadOnlyList<WishlistItemDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetWishlistQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WishlistItemDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return new List<WishlistItemDto>();

        return await _db.Wishlists
            .AsNoTracking()
            .Include(w => w.Product)
                .ThenInclude(p => p.Images)
            .Where(w => w.UserId == _currentUser.UserId.Value && w.Product.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemDto(
                w.Id,
                w.ProductId,
                w.Product.Name,
                w.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                w.Product.Price,
                w.Product.SalePrice,
                w.Product.PriceType,
                w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
