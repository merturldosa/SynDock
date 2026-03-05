using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Points.Queries;

public record GetPointBalanceQuery : IRequest<Result<PointBalanceDto>>;

public class GetPointBalanceQueryHandler : IRequestHandler<GetPointBalanceQuery, Result<PointBalanceDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPointBalanceQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PointBalanceDto>> Handle(GetPointBalanceQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<PointBalanceDto>.Failure("Authentication required.");

        var userPoint = await _db.UserPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(up => up.UserId == _currentUser.UserId.Value, cancellationToken);

        return Result<PointBalanceDto>.Success(new PointBalanceDto(userPoint?.Balance ?? 0));
    }
}
