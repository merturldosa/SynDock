using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Payments.Commands;

public record RefundPaymentCommand(
    int OrderId,
    string Reason
) : IRequest<Result<bool>>;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefundPaymentCommandHandler(
        IShopDbContext db,
        ICurrentUserService currentUser,
        IPaymentProvider paymentProvider,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _paymentProvider = paymentProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Variant)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("Order not found.");

        if (order.Status == nameof(OrderStatus.Refunded))
            return Result<bool>.Failure("Order has already been refunded.");

        if (order.Status == nameof(OrderStatus.Cancelled))
            return Result<bool>.Failure("Cancelled orders cannot be refunded.");

        // Find the completed payment
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.OrderId == order.Id && p.Status == nameof(PaymentStatus.Completed), cancellationToken);

        if (payment is not null && !string.IsNullOrEmpty(payment.PaymentKey))
        {
            // Call payment provider to cancel/refund
            var cancelResult = await _paymentProvider.CancelPayment(payment.PaymentKey, request.Reason, cancellationToken);
            if (!cancelResult.IsSuccess)
                return Result<bool>.Failure(cancelResult.Error ?? "Refund processing failed.");

            payment.Status = nameof(PaymentStatus.Refunded);
            payment.UpdatedBy = _currentUser.Username;
            payment.UpdatedAt = DateTime.UtcNow;
        }

        // Update order status
        order.Status = nameof(OrderStatus.Refunded);
        order.UpdatedBy = _currentUser.Username;
        order.UpdatedAt = DateTime.UtcNow;

        // Restore stock
        foreach (var item in order.Items)
        {
            if (item.VariantId.HasValue && item.Variant is not null)
            {
                item.Variant.Stock += item.Quantity;
            }
        }

        // Restore points
        if (order.PointsUsed > 0)
        {
            var userPoint = await _db.UserPoints
                .FirstOrDefaultAsync(up => up.UserId == order.UserId, cancellationToken);

            if (userPoint is not null)
            {
                userPoint.Balance += order.PointsUsed;
                await _db.PointHistories.AddAsync(new PointHistory
                {
                    UserId = order.UserId,
                    Amount = order.PointsUsed,
                    TransactionType = nameof(PointTransactionType.Refund),
                    Description = $"주문 환불 ({order.OrderNumber})",
                    CreatedBy = _currentUser.Username ?? "system"
                }, cancellationToken);
            }
        }

        // Restore coupon
        if (order.CouponId.HasValue)
        {
            var userCoupon = await _db.UserCoupons
                .FirstOrDefaultAsync(uc => uc.UsedOrderId == order.Id, cancellationToken);

            if (userCoupon is not null)
            {
                userCoupon.IsUsed = false;
                userCoupon.UsedAt = null;
                userCoupon.UsedOrderId = null;

                var coupon = await _db.Coupons.FindAsync(new object[] { order.CouponId.Value }, cancellationToken);
                if (coupon is not null)
                    coupon.CurrentUsageCount = Math.Max(0, coupon.CurrentUsageCount - 1);
            }
        }

        // Create order history
        await _db.OrderHistories.AddAsync(new OrderHistory
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Refunded),
            Note = $"환불 처리: {request.Reason}",
            CreatedBy = _currentUser.Username ?? "system"
        }, cancellationToken);

        // Create notification
        await _db.Notifications.AddAsync(new Notification
        {
            UserId = order.UserId,
            Type = nameof(NotificationType.Order),
            Title = "환불 완료",
            Message = $"주문 {order.OrderNumber}이(가) 환불되었습니다. 사유: {request.Reason}",
            ReferenceId = order.Id,
            ReferenceType = "Order",
            CreatedBy = "system"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
