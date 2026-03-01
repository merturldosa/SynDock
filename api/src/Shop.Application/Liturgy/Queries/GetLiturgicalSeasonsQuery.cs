using MediatR;
using Shop.Application.Common.DTOs;
using Shop.Application.Liturgy.Services;

namespace Shop.Application.Liturgy.Queries;

public record GetLiturgicalSeasonsQuery(int Year) : IRequest<IReadOnlyList<LiturgicalSeasonDto>>;

public class GetLiturgicalSeasonsQueryHandler : IRequestHandler<GetLiturgicalSeasonsQuery, IReadOnlyList<LiturgicalSeasonDto>>
{
    private readonly ILiturgicalCalendarService _liturgyService;

    public GetLiturgicalSeasonsQueryHandler(ILiturgicalCalendarService liturgyService)
    {
        _liturgyService = liturgyService;
    }

    public Task<IReadOnlyList<LiturgicalSeasonDto>> Handle(GetLiturgicalSeasonsQuery request, CancellationToken cancellationToken)
    {
        var seasons = _liturgyService.CalculateSeasons(request.Year);
        return Task.FromResult<IReadOnlyList<LiturgicalSeasonDto>>(seasons);
    }
}
