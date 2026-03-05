using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Wishlists.Commands;

public record ShareWishlistCommand : IRequest<Result<string>>;

public class ShareWishlistCommandHandler : IRequestHandler<ShareWishlistCommand, Result<string>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ShareWishlistCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(ShareWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var items = await _db.Wishlists.Where(w => w.UserId == userId).ToListAsync(cancellationToken);
        if (!items.Any())
            return Result<string>.Failure("Wishlist is empty.");

        var token = Guid.NewGuid();
        foreach (var item in items)
        {
            item.ShareToken = token;
            item.IsPublic = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<string>.Success(token.ToString());
    }
}
