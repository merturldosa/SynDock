using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Addresses.Commands;

public record CreateAddressCommand(
    string RecipientName,
    string Phone,
    string ZipCode,
    string Address1,
    string? Address2,
    bool IsDefault
) : IRequest<Result<int>>;

public class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAddressCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        // If setting as default, clear existing defaults
        if (request.IsDefault)
        {
            var existingDefaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
                addr.UpdatedBy = _currentUser.Username;
                addr.UpdatedAt = DateTime.UtcNow;
            }
        }

        var address = new Address
        {
            UserId = userId,
            RecipientName = request.RecipientName,
            Phone = request.Phone,
            ZipCode = request.ZipCode,
            Address1 = request.Address1,
            Address2 = request.Address2,
            IsDefault = request.IsDefault,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Addresses.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(address.Id);
    }
}
