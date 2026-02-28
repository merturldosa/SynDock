using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Posts.Queries;

public record GetFeedQuery(
    int Page = 1,
    int PageSize = 20,
    string? PostType = null,
    int? UserId = null
) : IRequest<PagedPostResult>;

public class GetFeedQueryHandler : IRequestHandler<GetFeedQuery, PagedPostResult>
{
    private readonly IShopDbContext _db;

    public GetFeedQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedPostResult> Handle(GetFeedQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Posts
            .AsNoTracking()
            .Where(p => p.IsVisible);

        if (!string.IsNullOrEmpty(request.PostType))
            query = query.Where(p => p.PostType == request.PostType);

        if (request.UserId.HasValue)
            query = query.Where(p => p.UserId == request.UserId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PostSummaryDto(
                p.Id,
                p.UserId,
                p.User.Name ?? p.User.Username,
                p.Title,
                p.Content.Length > 200 ? p.Content.Substring(0, 200) + "..." : p.Content,
                p.PostType,
                p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                p.ReactionCount,
                p.CommentCount,
                p.PostHashtags.Select(ph => ph.Hashtag.Tag).ToList(),
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedPostResult(totalCount, request.Page, request.PageSize, items);
    }
}
