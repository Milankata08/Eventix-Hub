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
        Assert.Equal("09.10.2026", Format.Date(D));
        Assert.Equal("09.10.2026 20:00", Format.DateAndTime(D));
        Assert.Equal("09:00", Format.Hour(9));
    }

    [Fact]
    public void Units_stay_with_their_numbers()
    {
        Assert.Equal("35\u00A0%", Format.Percent(35));
        Assert.Equal("120\u00A0мин.", Format.Minutes(120));
        Assert.Equal("№\u00A042", Format.OrderNo(42));
    }

    [Theory]
    [InlineData(0, "0 оценки")]
    [InlineData(1, "1 оценка")]
    [InlineData(2, "2 оценки")]
    [InlineData(11, "11 оценки")]
    public void Count_uses_singular_only_for_one(int n, string expected) =>
        Assert.Equal(expected, Format.Count(n, "оценка", "оценки"));

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

    [Theory]
    [InlineData("Морска зала Варна", "Морска зала Варна", "Варна")]
    [InlineData("Голяма зала Пловдив", "Голяма зала Пловдив", "Пловдив")]
    [InlineData("Зала 1 – Арена Сердика, София", "Зала 1 – Арена Сердика", "София")]
    [InlineData("Зала Варна", "Зала Варна", "варна")]
    [InlineData("Камерна", "Камерна", null)]
    [InlineData("София", null, "София")]
    [InlineData("", null, null)]
    public void Place_drops_city_already_in_venue_name(string expected, string? venue, string? city) =>
        Assert.Equal(expected, Format.Place(venue, city));

    [Theory]
    [InlineData(EventType.Movie, "започнала прожекция")]
    [InlineData(EventType.Concert, "започнал концерт")]
    [InlineData(EventType.Theatre, "започнало представление")]
    public void Started_showtime_agrees_in_gender(EventType t, string expected) =>
        Assert.Equal(expected, t.StartedShowtime());

    [Theory]
    [InlineData(ConcessionType.Regular, "Редовен")]
    [InlineData(ConcessionType.Student, "Студент −20 %")]
    [InlineData(ConcessionType.Pensioner, "Пенсионер −30 %")]
    public void Concession_names_the_discount(ConcessionType c, string expected) =>
        Assert.Equal(expected, Format.Concession(c));

    [Theory]
    [InlineData(SeatCategory.Vip, ConcessionType.Regular, "VIP")]
    [InlineData(SeatCategory.Standard, ConcessionType.Student, "Стандартно · Студент −20 %")]
    public void Fare_adds_the_concession_only_when_discounted(SeatCategory s, ConcessionType c, string expected) =>
        Assert.Equal(expected, Format.Fare(s, c));

    [Fact]
    public void PromoNote_names_the_code_and_percent_or_nothing()
    {
        Assert.Equal("с промо код WELCOME10 (−10 %)", Format.PromoNote("WELCOME10", 10));
        Assert.Null(Format.PromoNote(null, 0));
    }
}
