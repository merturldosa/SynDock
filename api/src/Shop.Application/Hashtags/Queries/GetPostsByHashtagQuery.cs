using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Hashtags.Queries;

public record GetPostsByHashtagQuery(string Tag, int Page = 1, int PageSize = 20) : IRequest<PagedPostResult>;

public class GetPostsByHashtagQueryHandler : IRequestHandler<GetPostsByHashtagQuery, PagedPostResult>
{
    private readonly IShopDbContext _db;

    public GetPostsByHashtagQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedPostResult> Handle(GetPostsByHashtagQuery request, CancellationToken cancellationToken)
    {
        var tag = request.Tag.Trim().ToLowerInvariant();

        var query = _db.PostHashtags
            .AsNoTracking()
            .Where(ph => ph.Hashtag.Tag == tag && ph.Post.IsVisible)
            .Select(ph => ph.Post);

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
