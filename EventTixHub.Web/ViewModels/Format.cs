using System.Globalization;

namespace EventTixHub.Web.ViewModels;

// Presentation-only bg-BG formats. Model binding and CSV export stay invariant.
public static class Format
{
    private static readonly CultureInfo Bg = CultureInfo.GetCultureInfo("bg-BG");

    // Non-breaking space so "12,50 €" never wraps.
    public static string Money(decimal value) => value.ToString("0.00", Bg) + " €";

    public static string Rating(double value) => value.ToString("0.0", Bg);

    public static string PosterDay(DateTime d) => d.ToString("dd", Bg);

    public static string PosterMonth(DateTime d) =>
        Bg.DateTimeFormat.AbbreviatedMonthNames[d.Month - 1].TrimEnd('.').ToUpper(Bg);

    public static string LongDate(DateTime d) => d.Day + " " + Bg.DateTimeFormat.MonthNames[d.Month - 1];

    public static string WeekdayShort(DateTime d) =>
        Bg.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek).TrimEnd('.').ToUpper(Bg);

    public static string Weekday(DateTime d) => Bg.DateTimeFormat.GetDayName(d.DayOfWeek);

    public static string Time(DateTime d) => d.ToString("HH:mm", Bg);

    // "във Варна, Пловдив и София": "във" before a city starting with в or ф, else "в". Empty for no cities.
    public static string CityPhrase(IReadOnlyList<string> cities)
    {
        if (cities.Count == 0) return "";
        var list = cities.Count == 1 ? cities[0] : string.Join(", ", cities.Take(cities.Count - 1)) + " и " + cities[^1];
        return (char.ToLower(cities[0][0], Bg) is 'в' or 'ф' ? "във " : "в ") + list;
    }

    // "Камерна, София", but "Морска зала Варна" when the venue name already names the city.
    public static string Place(string? venue, string? city) =>
        string.IsNullOrEmpty(city) || (venue?.Contains(city, StringComparison.OrdinalIgnoreCase) ?? false)
            ? venue ?? ""
            : string.IsNullOrEmpty(venue) ? city : venue + ", " + city;

    // Character count of the longest word; drives the poster title fit. At least 1.
    public static int LongestWord(string title) =>
        Math.Max(1, title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(w => w.Length).DefaultIfEmpty(1).Max());
}
