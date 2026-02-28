using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Wishlists.Commands;

public record ToggleWishlistCommand(int ProductId) : IRequest<Result<bool>>;

public class ToggleWishlistCommandHandler : IRequestHandler<ToggleWishlistCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleWishlistCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ToggleWishlistCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        var existing = await _db.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken);

        if (existing is not null)
        {
            _db.Wishlists.Remove(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(false); // removed
        }

        var productExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
            return Result<bool>.Failure("상품을 찾을 수 없습니다.");

        var wishlist = new Wishlist
        {
            UserId = userId,
            ProductId = request.ProductId,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Wishlists.AddAsync(wishlist, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true); // added
    }
}
