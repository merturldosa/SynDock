using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record AcceptDeliveryCommand(int AssignmentId) : IRequest<Result<bool>>;

public class AcceptDeliveryCommandHandler : IRequestHandler<AcceptDeliveryCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDriverLocationService _locationService;
    private readonly IDriverNotifier _driverNotifier;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptDeliveryCommandHandler(IShopDbContext db, ICurrentUserService currentUser,
        IDriverLocationService locationService, IDriverNotifier driverNotifier,
        INotificationSender notificationSender, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _locationService = locationService;
        _driverNotifier = driverNotifier;
        _notificationSender = notificationSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(AcceptDeliveryCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var driver = await _db.DeliveryDrivers
            .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId.Value, cancellationToken);

        if (driver is null)
            return Result<bool>.Failure("Driver profile not found.");

        var assignment = await _db.DeliveryAssignments
            .Include(a => a.Order)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);

        if (assignment is null)
            return Result<bool>.Failure("Delivery assignment not found.");

        if (assignment.Status != nameof(DeliveryAssignmentStatus.Pending) &&
            assignment.Status != nameof(DeliveryAssignmentStatus.Offered))
            return Result<bool>.Failure($"Assignment cannot be accepted in status: {assignment.Status}");

        // Update assignment
        assignment.DeliveryDriverId = driver.Id;
        assignment.Status = nameof(DeliveryAssignmentStatus.Accepted);
        assignment.AcceptedAt = DateTime.UtcNow;

        var location = _locationService.GetLatestLocation(driver.Id);
        if (location.HasValue)
        {
            assignment.AcceptLatitude = location.Value.Lat;
            assignment.AcceptLongitude = location.Value.Lng;
        }

        // Calculate estimated delivery time
        if (assignment.DeliveryOption is not null || assignment.DeliveryOptionId.HasValue)
        {
            var option = assignment.DeliveryOption ?? await _db.DeliveryOptions
                .FirstOrDefaultAsync(o => o.Id == assignment.DeliveryOptionId, cancellationToken);
            if (option is not null)
                assignment.EstimatedDeliveryAt = DateTime.UtcNow.AddMinutes(option.MaxDeliveryMinutes);
        }

        // Update driver status
        driver.Status = nameof(DriverStatus.Delivering);
        driver.UpdatedBy = _currentUser.Username ?? "system";
        driver.UpdatedAt = DateTime.UtcNow;

        assignment.UpdatedBy = _currentUser.Username ?? "system";
        assignment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify customer
        await _driverNotifier.NotifyDeliveryStatusChanged(assignment.Id, assignment.Status, cancellationToken);
        await _notificationSender.SendToUser(assignment.Order.UserId, new
        {
            type = "Delivery",
            title = "배송기사 배정",
            message = $"주문 {assignment.Order.OrderNumber}에 배송기사가 배정되었습니다.",
            assignmentId = assignment.Id
        }, cancellationToken);

        return Result<bool>.Success(true);
    }
}
