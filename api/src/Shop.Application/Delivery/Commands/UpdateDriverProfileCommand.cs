using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record UpdateDriverProfileCommand(
    string? Phone,
    string? VehicleType,
    string? LicensePlate,
    string? LicenseNumber
) : IRequest<Result<bool>>;

public class UpdateDriverProfileCommandHandler : IRequestHandler<UpdateDriverProfileCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDriverProfileCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateDriverProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var driver = await _db.DeliveryDrivers
            .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId.Value, cancellationToken);

        if (driver is null)
            return Result<bool>.Failure("Driver profile not found.");

        if (request.Phone is not null) driver.Phone = request.Phone;
        if (request.VehicleType is not null) driver.VehicleType = request.VehicleType;
        if (request.LicensePlate is not null) driver.LicensePlate = request.LicensePlate;
        if (request.LicenseNumber is not null) driver.LicenseNumber = request.LicenseNumber;

        driver.UpdatedBy = _currentUser.Username ?? "system";
        driver.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
