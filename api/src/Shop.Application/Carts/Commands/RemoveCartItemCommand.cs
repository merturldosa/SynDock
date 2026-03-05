using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Carts.Commands;

public record RemoveCartItemCommand(int CartItemId) : IRequest<Result<bool>>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCartItemCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var cartItem = await _db.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == _currentUser.UserId.Value, cancellationToken);

        if (cartItem is null)
            return Result<bool>.Failure("Cart item not found.");

        _db.CartItems.Remove(cartItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
