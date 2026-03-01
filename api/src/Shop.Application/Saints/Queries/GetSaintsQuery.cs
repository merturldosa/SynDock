using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Saints.Queries;

public record GetSaintsQuery(
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedList<SaintSummaryDto>>;

public class GetSaintsQueryHandler : IRequestHandler<GetSaintsQuery, PagedList<SaintSummaryDto>>
{
    private readonly IShopDbContext _db;

    public GetSaintsQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<PagedList<SaintSummaryDto>> Handle(GetSaintsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Saints
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(s =>
                s.KoreanName.ToLower().Contains(search) ||
                (s.LatinName != null && s.LatinName.ToLower().Contains(search)) ||
                (s.EnglishName != null && s.EnglishName.ToLower().Contains(search)));
        }

        query = query.OrderBy(s => s.KoreanName);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SaintSummaryDto(
                s.Id, s.KoreanName, s.LatinName, s.FeastDay, s.Patronage))
            .ToListAsync(cancellationToken);

        return new PagedList<SaintSummaryDto>(items, totalCount, request.Page, request.PageSize);
    }
}
