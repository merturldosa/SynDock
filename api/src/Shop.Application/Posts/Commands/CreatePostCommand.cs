using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Posts.Commands;

public record CreatePostCommand(
    string? Title,
    string Content,
    string PostType,
    int? ProductId,
    List<string>? ImageUrls,
    List<string>? Hashtags
) : IRequest<Result<int>>;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePostCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result<int>.Failure("Content is required.");

        var post = new Post
        {
            UserId = _currentUser.UserId.Value,
            Title = request.Title,
            Content = request.Content,
            PostType = request.PostType ?? "general",
            ProductId = request.ProductId,
            CreatedBy = _currentUser.Username ?? "system"
        };

        // Add images
        if (request.ImageUrls?.Count > 0)
        {
            for (int i = 0; i < request.ImageUrls.Count; i++)
            {
                post.Images.Add(new PostImage
                {
                    Url = request.ImageUrls[i],
                    SortOrder = i,
                    CreatedBy = _currentUser.Username ?? "system"
                });
            }
        }

        await _db.Posts.AddAsync(post, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Process hashtags (batch-load existing to avoid N+1)
        if (request.Hashtags?.Count > 0)
        {
            var tags = request.Hashtags.Select(t => t.Trim().ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
            var existingHashtags = await _db.Hashtags
                .Where(h => tags.Contains(h.Tag))
                .ToDictionaryAsync(h => h.Tag, cancellationToken);

            foreach (var tagText in tags)
            {
                Hashtag hashtag;
                if (existingHashtags.TryGetValue(tagText, out var existing))
                {
                    existing.PostCount++;
                    hashtag = existing;
                }
                else
                {
                    hashtag = new Hashtag { Tag = tagText, PostCount = 1, CreatedBy = "system" };
                    await _db.Hashtags.AddAsync(hashtag, cancellationToken);
                }

                await _db.PostHashtags.AddAsync(new PostHashtag
                {
                    PostId = post.Id,
                    HashtagId = hashtag.Id,
                    CreatedBy = "system"
                }, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result<int>.Success(post.Id);
    }
}
