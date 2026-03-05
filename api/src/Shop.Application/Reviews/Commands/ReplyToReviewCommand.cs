using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Reviews.Commands;

public record ReplyToReviewCommand(int ReviewId, string Reply) : IRequest<Result<bool>>;

public class ReplyToReviewCommandHandler : IRequestHandler<ReplyToReviewCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ReplyToReviewCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ReplyToReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);
        if (review == null)
            return Result<bool>.Failure("Review not found.");

        review.AdminReply = request.Reply;
        review.AdminRepliedAt = DateTime.UtcNow;
        review.UpdatedBy = _currentUser.Username;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
