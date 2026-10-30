using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Html;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Web.ViewModels;

// Presentation-only bg-BG formats. Model binding and CSV export stay invariant.
public static class Format
{
    private static readonly CultureInfo Bg = CultureInfo.GetCultureInfo("bg-BG");

    // Non-breaking space so "12,50 €" never wraps.
    public static string Money(decimal value) => value.ToString("0.00", Bg) + " €";

    // "Студент −20 %" (non-breaking space before %); "Редовен" carries no discount.
    public static string Concession(ConcessionType c) =>
        BookingRules.ConcessionPercent(c) is var p and > 0 ? $"{c.DisplayName()} −{Percent(p)}" : c.DisplayName();

    // What a seat line charges for: "VIP", or "Стандартно · Студент −20 %" when a discount applies.
    public static string Fare(SeatCategory category, ConcessionType c) =>
        BookingRules.ConcessionPercent(c) > 0 ? $"{category.DisplayName()} · {Concession(c)}" : category.DisplayName();

    // "WELCOME10 (−10 %)": the cart's promo row and a paid ticket's promo note share this wording.
    // translate="no" so browser auto-translate leaves the code as typed.
    public static IHtmlContent Promo(string code, int percent) =>
        new HtmlString($"<span translate=\"no\">{WebUtility.HtmlEncode(code)} (−{Percent(percent)})</span>");

    // Why a paid ticket costs less than its fare: "с промо код WELCOME10 (−10 %)"; null when the order had no promo.
    public static IHtmlContent? PromoNote(string? code, int percent) =>
        code is null ? null : new HtmlString($"с промо код {Promo(code, percent)}");

    public static string Rating(double value) => value.ToString("0.0", Bg);

    public static string PosterDay(DateTime d) => d.ToString("dd", Bg);

    public static string PosterMonth(DateTime d) =>
        Bg.DateTimeFormat.AbbreviatedMonthNames[d.Month - 1].TrimEnd('.').ToUpper(Bg);

    public static string LongDate(DateTime d) => d.Day + " " + Bg.DateTimeFormat.MonthNames[d.Month - 1];

    public static string WeekdayShort(DateTime d) =>
        Bg.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek).TrimEnd('.').ToUpper(Bg);

    public static string Weekday(DateTime d) => Bg.DateTimeFormat.GetDayName(d.DayOfWeek);

    public static string Time(DateTime d) => d.ToString("HH:mm", Bg);

    // "09.10.2026": admin lists, review dates.
    public static string Date(DateTime d) => d.ToString("dd.MM.yyyy", Bg);

    public static string DateAndTime(DateTime d) => d.ToString("dd.MM.yyyy HH:mm", Bg);

    // An hour of the day as a clock time: 9 -> "09:00".
    public static string Hour(int hour) => $"{hour:00}:00";

    // The value an <input type="date"> and its query string expect; not for display.
    public static string? DateInput(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // Non-breaking spaces keep units with their numbers: "35 %", "120 мин.", "№ 42".
    public static string Percent(int value) => value + " %";

    public static string Minutes(int value) => value + " мин.";

    public static string OrderNo(int id) => "№ " + id;

    // "1 оценка", "0 оценки", "2 оценки": Bulgarian uses the singular only for exactly one.
    public static string Count(int n, string one, string many) => $"{n} {(n == 1 ? one : many)}";

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
