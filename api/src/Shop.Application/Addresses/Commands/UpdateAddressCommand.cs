using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Addresses.Commands;

public record UpdateAddressCommand(
    int AddressId,
    string RecipientName,
    string Phone,
    string ZipCode,
    string Address1,
    string? Address2,
    bool IsDefault
) : IRequest<Result<bool>>;

public class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAddressCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, cancellationToken);

        if (address is null)
            return Result<bool>.Failure("배송지를 찾을 수 없습니다.");

        // If setting as default, clear existing defaults
        if (request.IsDefault && !address.IsDefault)
        {
            var existingDefaults = await _db.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && a.Id != request.AddressId)
                .ToListAsync(cancellationToken);

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
                addr.UpdatedBy = _currentUser.Username;
                addr.UpdatedAt = DateTime.UtcNow;
            }
        }

        address.RecipientName = request.RecipientName;
        address.Phone = request.Phone;
        address.ZipCode = request.ZipCode;
        address.Address1 = request.Address1;
        address.Address2 = request.Address2;
        address.IsDefault = request.IsDefault;
        address.UpdatedBy = _currentUser.Username;
        address.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
