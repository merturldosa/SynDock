using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.DTOs;
using Shop.Application.Common.Interfaces;
using Shop.Application.Liturgy.Services;

namespace Shop.Application.Liturgy.Queries;

public record GetCurrentLiturgyQuery() : IRequest<LiturgyTodayDto>;

public class GetCurrentLiturgyQueryHandler : IRequestHandler<GetCurrentLiturgyQuery, LiturgyTodayDto>
{
    private readonly ILiturgicalCalendarService _liturgyService;
    private readonly IShopDbContext _db;

    public GetCurrentLiturgyQueryHandler(ILiturgicalCalendarService liturgyService, IShopDbContext db)
    {
        _liturgyService = liturgyService;
        _db = db;
    }

    public async Task<LiturgyTodayDto> Handle(GetCurrentLiturgyQuery request, CancellationToken cancellationToken)
    {
        var currentSeason = _liturgyService.GetCurrentSeason();

        var now = DateTime.UtcNow;
        var todaySaints = await _db.Saints
            .AsNoTracking()
            .Where(s => s.IsActive && s.FeastDay != null &&
                        s.FeastDay.Value.Month == now.Month &&
                        s.FeastDay.Value.Day == now.Day)
            .OrderBy(s => s.KoreanName)
            .Select(s => new SaintSummaryDto(
                s.Id, s.KoreanName, s.LatinName, s.FeastDay, s.Patronage))
            .ToListAsync(cancellationToken);

        return new LiturgyTodayDto(currentSeason, todaySaints);
    }
}
