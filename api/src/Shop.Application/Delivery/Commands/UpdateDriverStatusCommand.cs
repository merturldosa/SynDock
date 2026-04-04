using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record UpdateDriverStatusCommand(string Status) : IRequest<Result<bool>>;

public class UpdateDriverStatusCommandHandler : IRequestHandler<UpdateDriverStatusCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDriverLocationService _locationService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDriverStatusCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IDriverLocationService locationService, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _locationService = locationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateDriverStatusCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        if (!Enum.TryParse<DriverStatus>(request.Status, out _))
            return Result<bool>.Failure($"Invalid status: {request.Status}");

        var driver = await _db.DeliveryDrivers
            .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId.Value, cancellationToken);

        if (driver is null)
            return Result<bool>.Failure("Driver profile not found.");

        if (!driver.IsApproved)
            return Result<bool>.Failure("Driver is not approved yet.");

        driver.Status = request.Status;
        driver.UpdatedBy = _currentUser.Username ?? "system";
        driver.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
