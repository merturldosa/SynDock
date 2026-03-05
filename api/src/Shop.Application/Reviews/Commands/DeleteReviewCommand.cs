using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Reviews.Commands;

public record DeleteReviewCommand(int ReviewId) : IRequest<Result<bool>>;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result<bool>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReviewCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<bool>.Failure("Authentication required.");

        var review = await _db.Reviews
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId && r.UserId == _currentUser.UserId.Value, cancellationToken);

        if (review is null)
            return Result<bool>.Failure("Review not found.");

        _db.Reviews.Remove(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
