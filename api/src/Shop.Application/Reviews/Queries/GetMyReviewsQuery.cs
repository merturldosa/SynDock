using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;
using SynDock.Core.Interfaces;

namespace Shop.Application.Reviews.Queries;

public record MyReviewDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    int Rating,
    string? Content,
    string? ImageUrl,
    DateTime CreatedAt);

public record MyReviewsResult(
    IReadOnlyList<MyReviewDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record GetMyReviewsQuery(int Page = 1, int PageSize = 10) : IRequest<Result<MyReviewsResult>>;

public class GetMyReviewsQueryHandler : IRequestHandler<GetMyReviewsQuery, Result<MyReviewsResult>>
{
    private readonly IShopDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyReviewsQueryHandler(IShopDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<MyReviewsResult>> Handle(GetMyReviewsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<MyReviewsResult>.Failure("로그인이 필요합니다.");

        var userId = _currentUser.UserId.Value;

        var query = _db.Reviews
            .AsNoTracking()
            .Include(r => r.Product)
                .ThenInclude(p => p.Images)
            .Where(r => r.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new MyReviewDto(
                r.Id,
                r.ProductId,
                r.Product.Name,
                r.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                r.Rating,
                r.Content,
                r.ImageUrl,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<MyReviewsResult>.Success(
            new MyReviewsResult(items, totalCount, request.Page, request.PageSize));
    }
}
