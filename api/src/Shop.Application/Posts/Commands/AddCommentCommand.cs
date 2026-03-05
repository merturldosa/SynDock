using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Posts.Commands;

public record AddCommentCommand(
    int PostId,
    string Content,
    int? ParentId
) : IRequest<Result<int>>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public AddCommentCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Content))
            return Result<int>.Failure("Content is required.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);
        if (post == null)
            return Result<int>.Failure("Post not found.");

        // Validate parent comment (2-depth only)
        if (request.ParentId.HasValue)
        {
            var parent = await _db.PostComments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ParentId.Value && c.PostId == request.PostId, cancellationToken);

            if (parent == null)
                return Result<int>.Failure("Parent comment not found.");

            if (parent.ParentId != null)
                return Result<int>.Failure("Cannot reply to a nested comment.");
        }

        var comment = new PostComment
        {
            PostId = request.PostId,
            UserId = _currentUser.UserId.Value,
            Content = request.Content,
            ParentId = request.ParentId,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.PostComments.AddAsync(comment, cancellationToken);
        post.CommentCount++;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(comment.Id);
    }
}
