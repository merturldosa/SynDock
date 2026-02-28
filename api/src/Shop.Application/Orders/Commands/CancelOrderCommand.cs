using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Commands;

public record CancelOrderCommand(int OrderId) : IRequest<Result<bool>>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == _currentUser.UserId.Value, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("주문을 찾을 수 없습니다.");

        if (order.Status != nameof(OrderStatus.Pending) && order.Status != nameof(OrderStatus.Confirmed))
            return Result<bool>.Failure("취소할 수 없는 주문 상태입니다.");

        order.Status = nameof(OrderStatus.Cancelled);
        order.UpdatedBy = _currentUser.Username;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
