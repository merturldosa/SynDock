using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Reviews.Queries;

public record GetPhotoReviewsQuery(int? ProductId, int Page = 1, int PageSize = 10) : IRequest<PhotoReviewsResultDto>;

public record PhotoReviewsResultDto(int TotalCount, IReadOnlyList<ReviewDto> Reviews);

public class GetPhotoReviewsQueryHandler : IRequestHandler<GetPhotoReviewsQuery, PhotoReviewsResultDto>
{
    private readonly IShopDbContext _db;

    public GetPhotoReviewsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PhotoReviewsResultDto> Handle(GetPhotoReviewsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.IsVisible && r.ImageUrl != null);

        if (request.ProductId.HasValue)
            query = query.Where(r => r.ProductId == request.ProductId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewDto(
                r.Id, r.ProductId, r.UserId, r.User.Name,
                r.Rating, r.Content, r.ImageUrl, r.IsVisible, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PhotoReviewsResultDto(totalCount, reviews);
    }
}
