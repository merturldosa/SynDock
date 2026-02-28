using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Hashtags.Queries;

public record GetTrendingHashtagsQuery(int Limit = 20) : IRequest<IReadOnlyList<HashtagDto>>;

public class GetTrendingHashtagsQueryHandler : IRequestHandler<GetTrendingHashtagsQuery, IReadOnlyList<HashtagDto>>
{
    private readonly IShopDbContext _db;

    public GetTrendingHashtagsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<HashtagDto>> Handle(GetTrendingHashtagsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Hashtags
            .AsNoTracking()
            .Where(h => h.PostCount > 0)
            .OrderByDescending(h => h.PostCount)
            .Take(request.Limit)
            .Select(h => new HashtagDto(h.Id, h.Tag, h.PostCount))
            .ToListAsync(cancellationToken);
    }
}
