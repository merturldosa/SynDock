using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Reviews.Commands;

public record CreateReviewCommand(
    int ProductId,
    int Rating,
    string? Content
) : IRequest<Result<int>>;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<int>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateReviewCommandHandler(IShopDbContext db, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<int>.Failure("로그인이 필요합니다.");

        if (request.Rating < 1 || request.Rating > 5)
            return Result<int>.Failure("별점은 1~5 사이여야 합니다.");

        var productExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);

        if (!productExists)
            return Result<int>.Failure("상품을 찾을 수 없습니다.");

        // Check if user already reviewed this product
        var alreadyReviewed = await _db.Reviews
            .AsNoTracking()
            .AnyAsync(r => r.ProductId == request.ProductId && r.UserId == _currentUser.UserId.Value, cancellationToken);

        if (alreadyReviewed)
            return Result<int>.Failure("이미 리뷰를 작성하셨습니다.");

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = _currentUser.UserId.Value,
            Rating = request.Rating,
            Content = request.Content,
            IsVisible = true,
            CreatedBy = _currentUser.Username ?? "system"
        };

        await _db.Reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(review.Id);
    }
}
