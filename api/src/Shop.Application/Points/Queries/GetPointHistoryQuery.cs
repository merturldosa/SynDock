using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Points.Queries;

public record GetPointHistoryQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedPointHistory>>;

public class GetPointHistoryQueryHandler : IRequestHandler<GetPointHistoryQuery, Result<PagedPointHistory>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPointHistoryQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedPointHistory>> Handle(GetPointHistoryQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<PagedPointHistory>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        var query = _db.PointHistories
            .AsNoTracking()
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ph => new PointHistoryDto(
                ph.Id,
                ph.Amount,
                ph.TransactionType,
                ph.Description,
                ph.OrderId,
                ph.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedPointHistory>.Success(new PagedPointHistory(items, totalCount));
    }
}
