using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using Shop.Domain.Enums;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Delivery.Commands;

public record RegisterDriverCommand(
    string Phone,
    string VehicleType,
    string? LicensePlate,
    string? LicenseNumber
) : IRequest<Result<int>>;

public class RegisterDriverCommandHandler : IRequestHandler<RegisterDriverCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDriverCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(RegisterDriverCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        var exists = await _db.DeliveryDrivers
            .AnyAsync(d => d.UserId == userId, cancellationToken);

        if (exists)
            return Result<int>.Failure("Already registered as a driver.");

        // Update user role to Driver
        var user = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        user.Role = nameof(UserRole.Driver);
        user.UpdatedBy = _currentUser.Username ?? "system";
        user.UpdatedAt = DateTime.UtcNow;

        var driver = new DeliveryDriver
        {
            UserId = userId,
            Phone = request.Phone,
            VehicleType = request.VehicleType,
            LicensePlate = request.LicensePlate,
            LicenseNumber = request.LicenseNumber,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.DeliveryDrivers.AddAsync(driver, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(driver.Id);
    }
}
