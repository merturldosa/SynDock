using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Notifications.Queries;

public record GetNotificationsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedNotifications>>;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<PagedNotifications>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedNotifications>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<PagedNotifications>.Failure("Authentication required.");

        var userId = _currentUser.UserId.Value;

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.IsRead,
                n.ReadAt,
                n.ReferenceId,
                n.ReferenceType,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedNotifications>.Success(new PagedNotifications(items, totalCount));
    }
}
