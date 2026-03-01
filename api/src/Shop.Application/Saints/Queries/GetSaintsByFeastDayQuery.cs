using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;

namespace Shop.Application.Saints.Queries;

public record GetSaintsByFeastDayQuery(int Month, int Day) : IRequest<IReadOnlyList<SaintSummaryDto>>;

public class GetSaintsByFeastDayQueryHandler : IRequestHandler<GetSaintsByFeastDayQuery, IReadOnlyList<SaintSummaryDto>>
{
    private readonly IShopDbContext _db;

    public GetSaintsByFeastDayQueryHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SaintSummaryDto>> Handle(GetSaintsByFeastDayQuery request, CancellationToken cancellationToken)
    {
        var saints = await _db.Saints
            .AsNoTracking()
            .Where(s => s.IsActive && s.FeastDay != null &&
                        s.FeastDay.Value.Month == request.Month &&
                        s.FeastDay.Value.Day == request.Day)
            .OrderBy(s => s.KoreanName)
            .Select(s => new SaintSummaryDto(
                s.Id, s.KoreanName, s.LatinName, s.FeastDay, s.Patronage))
            .ToListAsync(cancellationToken);

        return saints;
    }
}
