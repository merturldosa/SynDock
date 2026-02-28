using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Wishlists.Queries;

public record CheckWishlistQuery(List<int> ProductIds) : IRequest<HashSet<int>>;

public class CheckWishlistQueryHandler : IRequestHandler<CheckWishlistQuery, HashSet<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CheckWishlistQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<HashSet<int>> Handle(CheckWishlistQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return new HashSet<int>();

        var wishedProductIds = await _db.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == _currentUser.UserId.Value && request.ProductIds.Contains(w.ProductId))
            .Select(w => w.ProductId)
            .ToListAsync(cancellationToken);

        return wishedProductIds.ToHashSet();
    }
}
