using EventTixHub.Web.Models;
using EventTixHub.Web.ViewModels;

namespace EventTixHub.Tests.Unit;

public class FormatTests
{
    private static readonly DateTime D = new(2026, 10, 9, 20, 0, 0);

    [Fact]
    public void Money_uses_comma_and_non_breaking_space() => Assert.Equal("12,50 €", Format.Money(12.5m));

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
    [InlineData(EventType.Movie, "прожекции", "ЕКРАН")]
    [InlineData(EventType.Concert, "концерти", "СЦЕНА")]
    [InlineData(EventType.Theatre, "представления", "СЦЕНА")]
    public void Type_wording(EventType t, string plural, string front)
    {
        Assert.Equal(plural, t.ShowtimeNoun(true));
        Assert.Equal(front, t.HallFront());
    }
}
