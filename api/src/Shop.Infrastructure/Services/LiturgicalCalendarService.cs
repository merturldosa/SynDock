using Shop.Application.Common.DTOs;
using Shop.Application.Liturgy.Services;

namespace Shop.Infrastructure.Services;

public class LiturgicalCalendarService : ILiturgicalCalendarService
{
    public DateTime CalculateEaster(int year)
    {
        // Anonymous Gregorian algorithm (Meeus/Jones/Butcher)
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateTime(year, month, day);
    }

    public List<LiturgicalSeasonDto> CalculateSeasons(int year)
    {
        var easter = CalculateEaster(year);
        var seasons = new List<LiturgicalSeasonDto>();

        // 1. Ordinary Time 1 (after Epiphany to day before Ash Wednesday)
        // Epiphany is celebrated on January 6 (or nearest Sunday in some countries)
        // Baptism of the Lord is the day after Epiphany (Monday)
        // Simplified: OT1 starts Jan 7
        var ordinaryTime1Start = new DateTime(year, 1, 7);
        var ashWednesday = easter.AddDays(-46);
        var ordinaryTime1End = ashWednesday.AddDays(-1);

        seasons.Add(new LiturgicalSeasonDto(
            "OrdinaryTime1", ordinaryTime1Start, ordinaryTime1End, "green"));

        // 2. Lent (Ash Wednesday to Holy Thursday evening)
        var holyThursday = easter.AddDays(-3);
        seasons.Add(new LiturgicalSeasonDto(
            "Lent", ashWednesday, holyThursday.AddDays(-1), "purple"));

        // 3. Triduum (Holy Thursday evening to Easter Sunday)
        seasons.Add(new LiturgicalSeasonDto(
            "Triduum", holyThursday, easter, "red"));

        // 4. Easter Season (Easter Sunday to Pentecost)
        var pentecost = easter.AddDays(49);
        seasons.Add(new LiturgicalSeasonDto(
            "Easter", easter, pentecost, "white"));

        // 5. Ordinary Time 2 (after Pentecost to Advent)
        var ordinaryTime2Start = pentecost.AddDays(1);
        // Advent starts 4 Sundays before Christmas
        var adventStart = GetAdventStart(year);
        var ordinaryTime2End = adventStart.AddDays(-1);
        seasons.Add(new LiturgicalSeasonDto(
            "OrdinaryTime2", ordinaryTime2Start, ordinaryTime2End, "green"));

        // 6. Advent (4 Sundays before Christmas to Dec 24)
        seasons.Add(new LiturgicalSeasonDto(
            "Advent", adventStart, new DateTime(year, 12, 24), "purple"));

        // 7. Christmas Season (Dec 25 to Epiphany next year, simplified to Jan 6)
        seasons.Add(new LiturgicalSeasonDto(
            "Christmas", new DateTime(year, 12, 25), new DateTime(year + 1, 1, 6), "white"));

        // Also add Christmas from previous year that extends into this year (Jan 1-6)
        // This is already covered by OrdinaryTime1 starting Jan 7

        return seasons.OrderBy(s => s.StartDate).ToList();
    }

    public LiturgicalSeasonDto GetCurrentSeason()
    {
        return GetCurrentSeason(DateTime.UtcNow);
    }

    public LiturgicalSeasonDto GetCurrentSeason(DateTime date)
    {
        // Check current year and previous year (for Christmas season spanning years)
        var seasons = CalculateSeasons(date.Year);
        var prevYearSeasons = CalculateSeasons(date.Year - 1);
        var allSeasons = prevYearSeasons.Concat(seasons).ToList();

        var dateOnly = date.Date;

        var currentSeason = allSeasons
            .FirstOrDefault(s => dateOnly >= s.StartDate.Date && dateOnly <= s.EndDate.Date);

        // Fallback to Ordinary Time
        return currentSeason ?? new LiturgicalSeasonDto("OrdinaryTime2", dateOnly, dateOnly, "green");
    }

    private static DateTime GetAdventStart(int year)
    {
        // Advent starts on the 4th Sunday before Christmas (Dec 25)
        // = Sunday closest to Nov 30 (St. Andrew's Day)
        var christmas = new DateTime(year, 12, 25);
        var christmasDayOfWeek = (int)christmas.DayOfWeek;
        // Days back to previous Sunday from Christmas
        var daysToSunday = christmasDayOfWeek == 0 ? 7 : christmasDayOfWeek;
        var fourthSundayBefore = christmas.AddDays(-daysToSunday - 21);
        return fourthSundayBefore;
    }
}
