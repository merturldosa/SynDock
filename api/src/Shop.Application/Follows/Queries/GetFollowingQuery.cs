using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Follows.Queries;

public record GetFollowingQuery(int UserId) : IRequest<IReadOnlyList<FollowDto>>;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, IReadOnlyList<FollowDto>>
{
    private readonly IShopDbContext _db;

    public GetFollowingQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FollowDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
    {
        return await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == request.UserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowDto(
                f.FollowingId,
                f.Following.Username,
                f.Following.Name,
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
