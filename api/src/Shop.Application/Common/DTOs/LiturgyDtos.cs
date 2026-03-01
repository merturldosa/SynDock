namespace Shop.Application.Common.DTOs;

public record LiturgicalSeasonDto(
    string SeasonName,
    DateTime StartDate,
    DateTime EndDate,
    string LiturgicalColor);

public record LiturgyTodayDto(
    LiturgicalSeasonDto CurrentSeason,
    IReadOnlyList<SaintSummaryDto> TodaySaints);
