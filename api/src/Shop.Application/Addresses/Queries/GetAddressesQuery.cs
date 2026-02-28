using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Interfaces;

namespace Shop.Application.Addresses.Queries;

public record GetAddressesQuery : IRequest<IReadOnlyList<AddressDto>>;

public class GetAddressesQueryHandler : IRequestHandler<GetAddressesQuery, IReadOnlyList<AddressDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAddressesQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AddressDto>> Handle(GetAddressesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return new List<AddressDto>();

        return await _db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.UserId.Value)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto(a.Id, a.RecipientName, a.Phone, a.ZipCode, a.Address1, a.Address2, a.IsDefault))
            .ToListAsync(cancellationToken);
    }
}
