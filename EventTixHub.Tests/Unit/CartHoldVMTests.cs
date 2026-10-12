using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;

namespace EventTixHub.Tests.Unit;

public class CartHoldVMTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0);

    private static CartLineDto Line(int showtimeId, int minutesLeft) => new(
        0, showtimeId, EventType.Movie, "Дюн", "Зала 1", Now.AddDays(1), 1, 1,
        SeatCategory.Standard, ConcessionType.Regular, 10m, 10m, Now.AddMinutes(minutesLeft));

    private static CartDto Cart(params CartLineDto[] lines) => new(1, lines, null, 0, 0m, null, []);

    [Fact]
    public void SeatsForThisShowtime_AndTheWholeCart_EachExpireOnTheirOwn()
    {
        var hold = CartHoldVM.From(Cart(Line(1, 9), Line(1, 8), Line(2, 3)), 1, Now)!;

        Assert.Equal([480, 540], hold.Here);
        Assert.Equal([180, 480, 540], hold.All);
    }

    [Fact]
    public void SeatsOnlyForOtherShowtimes_LeaveThisShowtimeEmpty()
    {
        var hold = CartHoldVM.From(Cart(Line(2, 3), Line(3, 7)), 1, Now)!;

        Assert.Empty(hold.Here);
        Assert.Equal([180, 420], hold.All);
    }

    [Fact]
    public void NoCartOrEmptyCart_IsNoHold() =>
        Assert.Equal((null, null), (CartHoldVM.From(null, 1, Now), CartHoldVM.From(Cart(), 1, Now)));
}
