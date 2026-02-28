using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Follows.Queries;

public record GetUserProfileQuery(int UserId) : IRequest<Result<SocialProfileDto>>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<SocialProfileDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUserProfileQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<SocialProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.IsActive, cancellationToken);

        if (user == null)
            return Result<SocialProfileDto>.Failure("사용자를 찾을 수 없습니다.");

        var postCount = await _db.Posts.CountAsync(p => p.UserId == request.UserId && p.IsVisible, cancellationToken);
        var followerCount = await _db.Follows.CountAsync(f => f.FollowingId == request.UserId, cancellationToken);
        var followingCount = await _db.Follows.CountAsync(f => f.FollowerId == request.UserId, cancellationToken);

        var isFollowing = false;
        if (_currentUser.UserId.HasValue && _currentUser.UserId.Value != request.UserId)
        {
            isFollowing = await _db.Follows
                .AnyAsync(f => f.FollowerId == _currentUser.UserId.Value && f.FollowingId == request.UserId, cancellationToken);
        }

        return Result<SocialProfileDto>.Success(new SocialProfileDto(
            user.Id,
            user.Username,
            user.Name,
            postCount,
            followerCount,
            followingCount,
            isFollowing));
    }
}
