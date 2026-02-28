using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Carts.Commands;

public record UpdateCartItemCommand(
    int CartItemId,
    int Quantity
) : IRequest<Result<bool>>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCartItemCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var cartItem = await _db.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == request.CartItemId && ci.Cart.UserId == _currentUser.UserId.Value, cancellationToken);

        if (cartItem is null)
            return Result<bool>.Failure("장바구니 항목을 찾을 수 없습니다.");

        if (request.Quantity <= 0)
        {
            _db.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedBy = _currentUser.Username;
            cartItem.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
