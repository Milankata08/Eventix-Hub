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

    public static string Weekday(DateTime d) => Bg.DateTimeFormat.GetDayName(d.DayOfWeek);

    public static string Time(DateTime d) => d.ToString("HH:mm", Bg);

    // Character count of the longest word; drives the poster title fit. At least 1.
    public static int LongestWord(string title) =>
        Math.Max(1, title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(w => w.Length).DefaultIfEmpty(1).Max());
}
