using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Posts.Commands;

public record DeletePostCommand(int PostId) : IRequest<Result<bool>>;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePostCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("로그인이 필요합니다.");

        var post = await _db.Posts
            .Include(p => p.PostHashtags)
            .ThenInclude(ph => ph.Hashtag)
            .FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

        if (post == null)
            return Result<bool>.Failure("게시글을 찾을 수 없습니다.");

        if (post.UserId != _currentUser.UserId.Value)
            return Result<bool>.Failure("본인의 게시글만 삭제할 수 있습니다.");

        // Decrement hashtag post counts
        foreach (var ph in post.PostHashtags)
        {
            if (ph.Hashtag.PostCount > 0)
                ph.Hashtag.PostCount--;
        }

        _db.Posts.Remove(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
