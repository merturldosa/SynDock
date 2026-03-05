using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Addresses.Commands;

public record DeleteAddressCommand(int AddressId) : IRequest<Result<bool>>;

public class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAddressCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == _currentUser.UserId.Value, cancellationToken);

        if (address is null)
            return Result<bool>.Failure("Shipping address not found.");

        _db.Addresses.Remove(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
