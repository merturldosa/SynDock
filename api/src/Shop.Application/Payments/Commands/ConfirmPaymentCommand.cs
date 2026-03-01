using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Payments.Commands;

public record ConfirmPaymentCommand(
    string PaymentKey,
    string OrderId,
    decimal Amount
) : IRequest<Result<int>>;

public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmPaymentCommandHandler(
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

    public async Task<Result<int>> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        // Find order by OrderNumber
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderId && o.UserId == userId, cancellationToken);

        if (order is null)
            return Result<int>.Failure("주문을 찾을 수 없습니다.");

        if (order.Status != nameof(OrderStatus.Pending))
            return Result<int>.Failure("결제 대기 상태의 주문만 결제할 수 있습니다.");

        // Verify amount matches
        if (order.TotalAmount != request.Amount)
            return Result<int>.Failure("결제 금액이 일치하지 않습니다.");

        // Call payment provider to verify/confirm
        var verifyResult = await _paymentProvider.VerifyPayment(
            request.PaymentKey, request.OrderId, request.Amount, cancellationToken);

        if (!verifyResult.IsSuccess)
        {
            // Record failed payment
            await _db.Payments.AddAsync(new Payment
            {
                OrderId = order.Id,
                PaymentMethod = "카드",
                Status = nameof(PaymentStatus.Failed),
                Amount = request.Amount,
                PaymentKey = request.PaymentKey,
                ProviderName = _paymentProvider.ProviderName,
                FailReason = verifyResult.Error,
                CreatedBy = _currentUser.Username ?? "system"
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<int>.Failure(verifyResult.Error ?? "결제 승인에 실패했습니다.");
        }

        // Create successful payment record
        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethod = "카드",
            Status = nameof(PaymentStatus.Completed),
            Amount = request.Amount,
            TransactionId = verifyResult.TransactionId,
            PaidAt = verifyResult.PaidAt,
            PaymentKey = request.PaymentKey,
            ProviderName = _paymentProvider.ProviderName,
            CreatedBy = _currentUser.Username ?? "system"
        };
        await _db.Payments.AddAsync(payment, cancellationToken);

        // Update order status to Confirmed
        order.Status = nameof(OrderStatus.Confirmed);
        order.UpdatedBy = _currentUser.Username;
        order.UpdatedAt = DateTime.UtcNow;

        // Create order history
        await _db.OrderHistories.AddAsync(new OrderHistory
        {
            OrderId = order.Id,
            Status = nameof(OrderStatus.Confirmed),
            Note = "결제가 완료되었습니다.",
            CreatedBy = _currentUser.Username ?? "system"
        }, cancellationToken);

        // Create notification
        await _db.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Type = nameof(NotificationType.Order),
            Title = "결제 완료",
            Message = $"주문 {order.OrderNumber}의 결제가 완료되었습니다.",
            ReferenceId = order.Id,
            ReferenceType = "Order",
            CreatedBy = "system"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(order.Id);
    }
}
