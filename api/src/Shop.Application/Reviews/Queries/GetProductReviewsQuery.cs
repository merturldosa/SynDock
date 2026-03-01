using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Reviews.Queries;

public record GetProductReviewsQuery(int ProductId, int Page = 1, int PageSize = 10) : IRequest<ReviewSummaryDto>;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, ReviewSummaryDto>
{
    private readonly IShopDbContext _db;

    public GetProductReviewsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewSummaryDto> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == request.ProductId && r.IsVisible);

        var totalCount = await query.CountAsync(cancellationToken);
        var avgRating = totalCount > 0
            ? await query.AverageAsync(r => r.Rating, cancellationToken)
            : 0;

        var photoReviewCount = await query.CountAsync(r => r.ImageUrl != null, cancellationToken);

        var ratingDistribution = await query
            .GroupBy(r => r.Rating)
            .Select(g => new RatingDistributionDto(g.Key, g.Count()))
            .OrderByDescending(d => d.Rating)
            .ToListAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewDto(
                r.Id, r.ProductId, r.UserId, r.User.Name,
                r.Rating, r.Content, r.ImageUrl, r.IsVisible, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ReviewSummaryDto(totalCount, avgRating, photoReviewCount, ratingDistribution, reviews);
    }
}
