using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Hashtags.Queries;

public record SearchHashtagsQuery(string Keyword, int Limit = 10) : IRequest<IReadOnlyList<HashtagDto>>;

public class SearchHashtagsQueryHandler : IRequestHandler<SearchHashtagsQuery, IReadOnlyList<HashtagDto>>
{
    private readonly IShopDbContext _db;

    public SearchHashtagsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<HashtagDto>> Handle(SearchHashtagsQuery request, CancellationToken cancellationToken)
    {
        var keyword = request.Keyword.Trim().ToLowerInvariant();

        return await _db.Hashtags
            .AsNoTracking()
            .Where(h => h.Tag.Contains(keyword))
            .OrderByDescending(h => h.PostCount)
            .Take(request.Limit)
            .Select(h => new HashtagDto(h.Id, h.Tag, h.PostCount))
            .ToListAsync(cancellationToken);
    }
}
