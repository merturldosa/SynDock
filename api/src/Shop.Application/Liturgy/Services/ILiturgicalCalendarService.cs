using Shop.Application.Common.DTOs;

namespace Shop.Application.Liturgy.Services;

public interface ILiturgicalCalendarService
{
    DateTime CalculateEaster(int year);
    List<LiturgicalSeasonDto> CalculateSeasons(int year);
    LiturgicalSeasonDto GetCurrentSeason();
    LiturgicalSeasonDto GetCurrentSeason(DateTime date);
}
