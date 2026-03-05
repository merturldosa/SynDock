using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Orders.Commands;

public record UpdateOrderStatusCommand(
    int OrderId,
    string Status,
    string? TrackingNumber = null,
    string? TrackingCarrier = null
) : IRequest<Result<bool>>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IKakaoAlimtalkService _alimtalk;
    private readonly IAdminDashboardNotifier _adminNotifier;
    private readonly IMediator _mediator;

    public UpdateOrderStatusCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork, IEmailService emailService, IKakaoAlimtalkService alimtalk, IAdminDashboardNotifier adminNotifier, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _alimtalk = alimtalk;
        _adminNotifier = adminNotifier;
        _mediator = mediator;
    }

    public async Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        if (!Enum.TryParse<OrderStatus>(request.Status, out _))
            return Result<bool>.Failure($"Invalid order status: {request.Status}");

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result<bool>.Failure("Order not found.");

        // Role-based state transition enforcement
        var isAdmin = _currentUser.Role == "TenantAdmin" || _currentUser.Role == "Admin" || _currentUser.Role == "PlatformAdmin";
        var isOwner = order.UserId == _currentUser.UserId.Value;

        if (!isAdmin && !isOwner)
            return Result<bool>.Failure("Access denied.");

        // Members can only cancel their own pending orders
        if (!isAdmin)
        {
            if (request.Status != nameof(OrderStatus.Cancelled))
                return Result<bool>.Failure("Access denied. Only administrators can change order status.");
            if (order.Status != nameof(OrderStatus.Pending))
                return Result<bool>.Failure("Only pending orders can be cancelled.");
        }

        // Validate status transition
        if (!IsValidTransition(order.Status, request.Status))
            return Result<bool>.Failure($"Cannot transition from '{order.Status}' to '{request.Status}'.");

        var previousStatus = order.Status;
        order.Status = request.Status;
        order.UpdatedBy = _currentUser.Username;
        order.UpdatedAt = DateTime.UtcNow;

        // Record order history
        var statusNote = GetStatusNote(request.Status, request.TrackingNumber);
        var history = new OrderHistory
        {
            OrderId = order.Id,
            Status = request.Status,
            Note = statusNote,
            TrackingNumber = request.TrackingNumber,
            TrackingCarrier = request.TrackingCarrier,
            CreatedBy = _currentUser.Username ?? "system"
        };
        await _db.OrderHistories.AddAsync(history, cancellationToken);

        // Create notification for user
        var notificationTitle = GetNotificationTitle(request.Status);
        var notificationMessage = GetNotificationMessage(request.Status, request.TrackingNumber, request.TrackingCarrier);
        var notification = new Notification
        {
            UserId = order.UserId,
            Type = nameof(NotificationType.Order),
            Title = notificationTitle,
            Message = notificationMessage,
            ReferenceId = order.Id,
            ReferenceType = "Order",
            CreatedBy = "system"
        };
        await _db.Notifications.AddAsync(notification, cancellationToken);

        // Send email + Kakao Alimtalk notifications
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId, cancellationToken);
        if (user is not null)
        {
            // Email
            if (!string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    if (request.Status == nameof(OrderStatus.Confirmed))
                    {
                        var emailBody = $"<h2>주문이 확인되었습니다</h2><p>주문번호: <strong>{order.OrderNumber}</strong></p><p>결제 금액: {order.TotalAmount:N0}원</p><p>상품 준비 후 발송해 드리겠습니다.</p>";
                        await _emailService.SendAsync(user.Email, "주문 확인", emailBody, cancellationToken);
                    }
                    else if (request.Status == nameof(OrderStatus.Shipped))
                    {
                        var trackingInfo = !string.IsNullOrEmpty(request.TrackingNumber)
                            ? $"<p>택배사: {request.TrackingCarrier ?? "택배"}<br/>운송장번호: <strong>{request.TrackingNumber}</strong></p>"
                            : "";
                        var emailBody = $"<h2>상품이 발송되었습니다</h2><p>주문번호: <strong>{order.OrderNumber}</strong></p>{trackingInfo}<p>배송 완료까지 2~3일 소요됩니다.</p>";
                        await _emailService.SendAsync(user.Email, "배송 시작", emailBody, cancellationToken);
                    }
                }
                catch { /* Email failure should not block order status update */ }
            }

            // Kakao Alimtalk
            if (!string.IsNullOrEmpty(user.Phone))
            {
                try
                {
                    _ = request.Status switch
                    {
                        nameof(OrderStatus.Confirmed) => await _alimtalk.SendOrderConfirmedAsync(user.Phone, order.OrderNumber, order.TotalAmount, cancellationToken),
                        nameof(OrderStatus.Shipped) => await _alimtalk.SendShippedAsync(user.Phone, order.OrderNumber, request.TrackingCarrier, request.TrackingNumber, cancellationToken),
                        nameof(OrderStatus.Delivered) => await _alimtalk.SendDeliveredAsync(user.Phone, order.OrderNumber, cancellationToken),
                        _ => false
                    };
                }
                catch { /* Alimtalk failure should not block order status update */ }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish MES order forwarding event for confirmed orders
        if (request.Status == nameof(OrderStatus.Confirmed))
        {
            try { await _mediator.Publish(new OrderConfirmedEvent(order.Id, order.TenantId, order.OrderNumber), cancellationToken); }
            catch { /* MES forwarding failure should not block order confirmation */ }
        }

        // Notify admin dashboard
        try { await _adminNotifier.NotifyOrderStatusChanged(order.TenantId, order.OrderNumber, request.Status, cancellationToken); }
        catch { /* notification failure should not block status update */ }

        return Result<bool>.Success(true);
    }

    private static bool IsValidTransition(string current, string next)
    {
        return (current, next) switch
        {
            (nameof(OrderStatus.Pending), nameof(OrderStatus.Confirmed)) => true,
            (nameof(OrderStatus.Pending), nameof(OrderStatus.Cancelled)) => true,
            (nameof(OrderStatus.Confirmed), nameof(OrderStatus.Processing)) => true,
            (nameof(OrderStatus.Confirmed), nameof(OrderStatus.Cancelled)) => true,
            (nameof(OrderStatus.Processing), nameof(OrderStatus.Shipped)) => true,
            (nameof(OrderStatus.Shipped), nameof(OrderStatus.Delivered)) => true,
            (nameof(OrderStatus.Delivered), nameof(OrderStatus.Refunded)) => true,
            (nameof(OrderStatus.Cancelled), nameof(OrderStatus.Refunded)) => true,
            _ => false
        };
    }

    private static string GetStatusNote(string status, string? trackingNumber) => status switch
    {
        nameof(OrderStatus.Confirmed) => "주문이 확인되었습니다.",
        nameof(OrderStatus.Processing) => "상품 준비가 시작되었습니다.",
        nameof(OrderStatus.Shipped) => string.IsNullOrEmpty(trackingNumber)
            ? "상품이 발송되었습니다."
            : $"상품이 발송되었습니다. (운송장: {trackingNumber})",
        nameof(OrderStatus.Delivered) => "배송이 완료되었습니다.",
        nameof(OrderStatus.Cancelled) => "주문이 취소되었습니다.",
        nameof(OrderStatus.Refunded) => "환불이 처리되었습니다.",
        _ => $"주문 상태가 {status}(으)로 변경되었습니다."
    };

    private static string GetNotificationTitle(string status) => status switch
    {
        nameof(OrderStatus.Confirmed) => "주문이 확인되었습니다",
        nameof(OrderStatus.Processing) => "상품 준비가 시작되었습니다",
        nameof(OrderStatus.Shipped) => "상품이 발송되었습니다",
        nameof(OrderStatus.Delivered) => "배송이 완료되었습니다",
        nameof(OrderStatus.Cancelled) => "주문이 취소되었습니다",
        nameof(OrderStatus.Refunded) => "환불이 처리되었습니다",
        _ => "주문 상태가 변경되었습니다"
    };

    private static string GetNotificationMessage(string status, string? trackingNumber, string? carrier)
    {
        if (status == nameof(OrderStatus.Shipped) && !string.IsNullOrEmpty(trackingNumber))
        {
            var carrierText = string.IsNullOrEmpty(carrier) ? "" : $"({carrier}) ";
            return $"상품이 발송되었습니다. {carrierText}운송장번호: {trackingNumber}";
        }
        return GetStatusNote(status, trackingNumber);
    }
}
