using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
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
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == _currentUser.UserId.Value, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("주문을 찾을 수 없습니다.");

        if (order.Status != nameof(OrderStatus.Pending) && order.Status != nameof(OrderStatus.Confirmed))
            return Result<bool>.Failure("취소할 수 없는 주문 상태입니다.");

        order.Status = nameof(OrderStatus.Cancelled);
        order.UpdatedBy = _currentUser.Username;
        order.UpdatedAt = DateTime.UtcNow;

        // Restore stock for variant items
        var variantIds = order.Items
            .Where(oi => oi.VariantId.HasValue)
            .Select(oi => oi.VariantId!.Value)
            .ToList();

        if (variantIds.Any())
        {
            var variants = await _db.ProductVariants
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items.Where(oi => oi.VariantId.HasValue))
            {
                var variant = variants.FirstOrDefault(v => v.Id == item.VariantId);
                if (variant is not null)
                    variant.Stock += item.Quantity;
            }
        }

        // Record cancellation history
        var history = new OrderHistory
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Cancelled),
            Note = "주문이 취소되었습니다.",
            CreatedBy = _currentUser.Username ?? "system"
        };
        await _db.OrderHistories.AddAsync(history, cancellationToken);

        // Create notification
        var notification = new Notification
        {
            UserId = order.UserId,
            Type = nameof(NotificationType.Order),
            Title = "주문이 취소되었습니다",
            Message = "주문이 취소 처리되었습니다.",
            ReferenceId = order.Id,
            ReferenceType = "Order",
            CreatedBy = "system"
        };
        await _db.Notifications.AddAsync(notification, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
