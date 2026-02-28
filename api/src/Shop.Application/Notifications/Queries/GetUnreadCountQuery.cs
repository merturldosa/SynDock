using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Queries;

public record GetUnreadCountQuery : IRequest<Result<UnreadCountDto>>;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<UnreadCountDto>.Failure("로그인이 필요합니다.");

        var count = await _db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == _currentUser.UserId.Value && !n.IsRead, cancellationToken);

        return Result<UnreadCountDto>.Success(new UnreadCountDto(count));
    }
}
