using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Queries;

public record DeliveryTrackingDto(
    int AssignmentId,
    string Status,
    string? DriverName,
    string? DriverPhone,
    string? VehicleType,
    string? LicensePlate,
    double? DriverLatitude,
    double? DriverLongitude,
    DateTime? EstimatedDeliveryAt,
    DateTime? AcceptedAt,
    DateTime? PickedUpAt,
    DateTime? InTransitAt,
    DateTime? DeliveredAt,
    string? DeliveryPhotoUrl,
    string? DeliveryNote,
    string? DeliveryType,
    string? DeliveryOptionName
);

public record GetDeliveryTrackingQuery(int OrderId) : IRequest<Result<DeliveryTrackingDto>>;

public class GetDeliveryTrackingQueryHandler : IRequestHandler<GetDeliveryTrackingQuery, Result<DeliveryTrackingDto>>
{
    private readonly IShopDbContext _db;
    private readonly IDriverLocationService _locationService;
    private readonly ICurrentUserService _currentUser;

    public GetDeliveryTrackingQueryHandler(IShopDbContext db, IDriverLocationService locationService, ICurrentUserService currentUser)
    {
        _db = db;
        _locationService = locationService;
        _currentUser = currentUser;
    }

    public async Task<Result<DeliveryTrackingDto>> Handle(GetDeliveryTrackingQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<DeliveryTrackingDto>.Failure("Authentication required.");

        var assignment = await _db.DeliveryAssignments
            .Include(a => a.Driver).ThenInclude(d => d!.User)
            .Include(a => a.DeliveryOption)
            .Include(a => a.Order)
            .Where(a => a.OrderId == request.OrderId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
            return Result<DeliveryTrackingDto>.Failure("No delivery assignment found for this order.");

        // Verify ownership (customer or admin)
        var isOwner = assignment.Order.UserId == _currentUser.UserId.Value;
        var isAdmin = _currentUser.Role is "TenantAdmin" or "Admin" or "PlatformAdmin";
        var isDriver = assignment.Driver?.UserId == _currentUser.UserId.Value;

        if (!isOwner && !isAdmin && !isDriver)
            return Result<DeliveryTrackingDto>.Failure("Access denied.");

        double? driverLat = null, driverLng = null;
        if (assignment.DeliveryDriverId.HasValue)
        {
            var loc = _locationService.GetLatestLocation(assignment.DeliveryDriverId.Value);
            if (loc.HasValue)
            {
                driverLat = loc.Value.Lat;
                driverLng = loc.Value.Lng;
            }
        }

        var dto = new DeliveryTrackingDto(
            AssignmentId: assignment.Id,
            Status: assignment.Status,
            DriverName: assignment.Driver?.User.Username,
            DriverPhone: assignment.Driver?.Phone,
            VehicleType: assignment.Driver?.VehicleType,
            LicensePlate: assignment.Driver?.LicensePlate,
            DriverLatitude: driverLat,
            DriverLongitude: driverLng,
            EstimatedDeliveryAt: assignment.EstimatedDeliveryAt,
            AcceptedAt: assignment.AcceptedAt,
            PickedUpAt: assignment.PickedUpAt,
            InTransitAt: assignment.InTransitAt,
            DeliveredAt: assignment.DeliveredAt,
            DeliveryPhotoUrl: assignment.DeliveryPhotoUrl,
            DeliveryNote: assignment.DeliveryNote,
            DeliveryType: assignment.DeliveryOption?.DeliveryType,
            DeliveryOptionName: assignment.DeliveryOption?.DisplayName
        );

        return Result<DeliveryTrackingDto>.Success(dto);
    }
}
