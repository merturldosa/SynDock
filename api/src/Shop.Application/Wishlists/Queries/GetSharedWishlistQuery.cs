using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Wishlists.Queries;

public record GetSharedWishlistQuery(Guid Token) : IRequest<List<SharedWishlistItemDto>>;

public record SharedWishlistItemDto(int ProductId, string ProductName, string? ImageUrl, decimal Price, string? Slug);

public class GetSharedWishlistQueryHandler : IRequestHandler<GetSharedWishlistQuery, List<SharedWishlistItemDto>>
{
    private readonly IShopDbContext _db;

    public GetSharedWishlistQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<List<SharedWishlistItemDto>> Handle(GetSharedWishlistQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Wishlists
            .Where(w => w.ShareToken == request.Token && w.IsPublic)
            .Include(w => w.Product)
            .ThenInclude(p => p.Images)
            .Select(w => new SharedWishlistItemDto(
                w.ProductId,
                w.Product.Name,
                w.Product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                w.Product.Price,
                w.Product.Slug))
            .ToListAsync(cancellationToken);

        return items;
    }
}
