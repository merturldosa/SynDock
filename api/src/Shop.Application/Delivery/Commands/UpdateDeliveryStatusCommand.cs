using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record UpdateDeliveryStatusCommand(
    int AssignmentId,
    string Status,
    string? PhotoUrl,
    string? Note
) : IRequest<Result<bool>>;

public class UpdateDeliveryStatusCommandHandler : IRequestHandler<UpdateDeliveryStatusCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDriverNotifier _driverNotifier;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDeliveryStatusCommandHandler(IShopDbContext db, ICurrentUserService currentUser,
        IDriverNotifier driverNotifier, INotificationSender notificationSender, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _driverNotifier = driverNotifier;
        _notificationSender = notificationSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateDeliveryStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        if (!Enum.TryParse<DeliveryAssignmentStatus>(request.Status, out var status))
            return Result<bool>.Failure($"Invalid status: {request.Status}");

        var assignment = await _db.DeliveryAssignments
            .Include(a => a.Order)
            .Include(a => a.Driver)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result<bool>.Failure("Delivery assignment not found.");

        // Validate driver owns this assignment
        var driver = await _db.DeliveryDrivers
            .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId.Value, cancellationToken);

        if (driver is null || assignment.DeliveryDriverId != driver.Id)
            return Result<bool>.Failure("You are not assigned to this delivery.");

        assignment.Status = request.Status;
        assignment.UpdatedBy = _currentUser.Username ?? "system";
        assignment.UpdatedAt = DateTime.UtcNow;

        switch (status)
        {
            case DeliveryAssignmentStatus.PickedUp:
                assignment.PickedUpAt = DateTime.UtcNow;
                assignment.Order.Status = nameof(OrderStatus.Shipped);
                break;
            case DeliveryAssignmentStatus.InTransit:
                assignment.InTransitAt = DateTime.UtcNow;
                break;
            case DeliveryAssignmentStatus.Delivered:
                assignment.DeliveredAt = DateTime.UtcNow;
                assignment.DeliveryPhotoUrl = request.PhotoUrl;
                assignment.DeliveryNote = request.Note;
                assignment.Order.Status = nameof(OrderStatus.Delivered);
                driver.Status = nameof(DriverStatus.Online);
                driver.TotalDeliveries++;
                break;
        }

        // Create order history
        var history = new OrderHistory
        {
            OrderId = assignment.OrderId,
            Status = assignment.Order.Status,
            Note = status switch
            {
                DeliveryAssignmentStatus.PickedUp => "배송기사가 상품을 수령했습니다.",
                DeliveryAssignmentStatus.InTransit => "배송이 시작되었습니다.",
                DeliveryAssignmentStatus.Delivered => "배송이 완료되었습니다.",
                _ => $"배송 상태: {request.Status}"
            },
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.OrderHistories.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Real-time notifications
        await _driverNotifier.NotifyDeliveryStatusChanged(assignment.Id, assignment.Status, cancellationToken);

        var notifMessage = status switch
        {
            DeliveryAssignmentStatus.PickedUp => $"주문 {assignment.Order.OrderNumber}: 배송기사가 상품을 수령했습니다.",
            DeliveryAssignmentStatus.InTransit => $"주문 {assignment.Order.OrderNumber}: 배송이 시작되었습니다.",
            DeliveryAssignmentStatus.Delivered => $"주문 {assignment.Order.OrderNumber}: 배송이 완료되었습니다.",
            _ => $"주문 {assignment.Order.OrderNumber}: 배송 상태가 변경되었습니다."
        };

        await _notificationSender.SendToUser(assignment.Order.UserId, new
        {
            type = "Delivery",
            title = "배송 상태 변경",
            message = notifMessage,
            assignmentId = assignment.Id
        }, cancellationToken);

        // Save in-app notification
        await _db.Notifications.AddAsync(new Notification
        {
            UserId = assignment.Order.UserId,
            Type = "Order",
            Title = "배송 상태 변경",
            Message = notifMessage,
            ReferenceId = assignment.OrderId,
            ReferenceType = "Order",
            CreatedBy = "system"
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
