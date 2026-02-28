using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Follows.Queries;

public record GetFollowersQuery(int UserId) : IRequest<IReadOnlyList<FollowDto>>;

public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, IReadOnlyList<FollowDto>>
{
    private readonly IShopDbContext _db;

    public GetFollowersQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FollowDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
    {
        return await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == request.UserId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowDto(
                f.FollowerId,
                f.Follower.Username,
                f.Follower.Name,
                f.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
