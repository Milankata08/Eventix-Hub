using EventTixHub.Web.Models;
using EventTixHub.Web.ViewModels;

namespace EventTixHub.Tests.Unit;

public class FormatTests
{
    private static readonly DateTime D = new(2026, 10, 9, 20, 0, 0);

    [Fact]
    public void Money_uses_comma_and_non_breaking_space() => Assert.Equal("12,50 €", Format.Money(12.5m));

    [Theory]
    [InlineData("Симфоничен оркестър: Бетовен", 10)]
    [InlineData("  Дюн  ", 3)]
    [InlineData("", 1)]
    public void LongestWord_counts_longest_whitespace_separated_word(string title, int expected) =>
        Assert.Equal(expected, Format.LongestWord(title));

    [Fact]
    public void Dates_read_in_bulgarian()
    {
        Assert.Equal("09", Format.PosterDay(D));
        Assert.Equal("ОКТ", Format.PosterMonth(D));
        Assert.Equal("9 октомври", Format.LongDate(D));
        Assert.Equal("петък", Format.Weekday(D));
        Assert.Equal("20:00", Format.Time(D));
    }

    [Theory]
    [InlineData("в Пловдив и София", "Пловдив", "София")]
    [InlineData("във Варна, Пловдив и София", "Варна", "Пловдив", "София")]
    [InlineData("в София", "София")]
    [InlineData("във Фрайбург и София", "Фрайбург", "София")]
    [InlineData("във велико Търново", "велико Търново")]
    [InlineData("", new string[0])]
    public void City_phrase_picks_preposition_and_joins(string expected, params string[] cities) =>
        Assert.Equal(expected, Format.CityPhrase(cities));

    [Theory]
    [InlineData(EventType.Movie, "прожекции", "ЕКРАН")]
    [InlineData(EventType.Concert, "концерти", "СЦЕНА")]
    [InlineData(EventType.Theatre, "представления", "СЦЕНА")]
    public void Type_wording(EventType t, string plural, string front)
    {
        Assert.Equal(plural, t.ShowtimeNoun(true));
        Assert.Equal(front, t.HallFront());
    }

    [Theory]
    [InlineData(EventType.Movie, "тази")]
    [InlineData(EventType.Concert, "този")]
    [InlineData(EventType.Theatre, "това")]
    public void Showtime_demonstrative_follows_gender(EventType t, string expected) =>
        Assert.Equal(expected, t.ShowtimeThis());
}
