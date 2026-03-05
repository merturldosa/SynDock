using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Posts.Commands;

public record ToggleReactionCommand(
    int PostId,
    string ReactionType
) : IRequest<Result<bool>>;

public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleReactionCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);
        if (post == null)
            return Result<bool>.Failure("Post not found.");

        var existing = await _db.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == request.PostId
                && r.UserId == _currentUser.UserId.Value
                && r.ReactionType == request.ReactionType, cancellationToken);

        if (existing != null)
        {
            _db.PostReactions.Remove(existing);
            post.ReactionCount = Math.Max(0, post.ReactionCount - 1);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(false); // removed
        }

        await _db.PostReactions.AddAsync(new PostReaction
        {
            PostId = request.PostId,
            UserId = _currentUser.UserId.Value,
            ReactionType = request.ReactionType,
            CreatedBy = _currentUser.Username ?? "system"
        }, cancellationToken);

        post.ReactionCount++;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true); // added
    }
}
