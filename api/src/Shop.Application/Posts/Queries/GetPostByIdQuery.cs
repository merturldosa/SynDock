using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Posts.Queries;

public record GetPostByIdQuery(int PostId) : IRequest<Result<PostDto>>;

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, Result<PostDto>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPostByIdQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PostDto>> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Product)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .Include(p => p.Comments.Where(c => c.ParentId == null && c.IsVisible))
                .ThenInclude(c => c.User)
            .Include(p => p.Comments.Where(c => c.ParentId == null && c.IsVisible))
                .ThenInclude(c => c.Replies.Where(r => r.IsVisible))
                    .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == request.PostId && p.IsVisible, cancellationToken);

        if (post == null)
            return Result<PostDto>.Failure("게시글을 찾을 수 없습니다.");

        // Get user's reaction
        string? myReaction = null;
        if (_currentUser.UserId.HasValue)
        {
            myReaction = await _db.PostReactions
                .AsNoTracking()
                .Where(r => r.PostId == request.PostId && r.UserId == _currentUser.UserId.Value)
                .Select(r => r.ReactionType)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var dto = new PostDto(
            post.Id,
            post.UserId,
            post.User.Name ?? post.User.Username,
            post.Title,
            post.Content,
            post.PostType,
            post.ProductId,
            post.Product?.Name,
            post.ViewCount,
            post.ReactionCount,
            post.CommentCount,
            post.Images.Select(i => new PostImageDto(i.Id, i.Url, i.AltText, i.SortOrder)).ToList(),
            post.PostHashtags.Select(ph => ph.Hashtag.Tag).ToList(),
            post.Comments
                .Where(c => c.ParentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new PostCommentDto(
                    c.Id, c.UserId, c.User.Name ?? c.User.Username, c.Content, null,
                    c.Replies.OrderBy(r => r.CreatedAt)
                        .Select(r => new PostCommentDto(r.Id, r.UserId, r.User.Name ?? r.User.Username, r.Content, c.Id, null, r.CreatedAt))
                        .ToList(),
                    c.CreatedAt))
                .ToList(),
            myReaction,
            post.CreatedAt);

        return Result<PostDto>.Success(dto);
    }
}
