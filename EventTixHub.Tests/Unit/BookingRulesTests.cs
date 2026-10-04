using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Unit;

public class BookingRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 12, 0, 0);

    [Theory]
    [InlineData(SeatCategory.Standard, 10.00)]
    [InlineData(SeatCategory.Vip, 20.00)]
    public void TicketPrice_UsesZonePrice(SeatCategory category, decimal expected) =>
        Assert.Equal(expected, BookingRules.TicketPrice(10m, 20m, category, ConcessionType.Regular, 0));

    [Fact]
    public void TicketPrice_StudentGets20PercentOff() =>
        Assert.Equal(9.60m, BookingRules.TicketPrice(12m, 12m, SeatCategory.Standard, ConcessionType.Student, 0));

    [Fact]
    public void TicketPrice_PensionerGets30PercentOff() =>
        Assert.Equal(8.40m, BookingRules.TicketPrice(12m, 12m, SeatCategory.Standard, ConcessionType.Pensioner, 0));

    [Fact]
    public void TicketPrice_PromoAppliesAfterConcession() =>
        Assert.Equal(14.40m, BookingRules.TicketPrice(10m, 20m, SeatCategory.Vip, ConcessionType.Student, 10));

    [Fact]
    public void TicketPrice_RoundsMidpointAwayFromZero() =>
        Assert.Equal(7.11m, BookingRules.TicketPrice(10.15m, 10.15m, SeatCategory.Standard, ConcessionType.Pensioner, 0));

    [Fact]
    public void OrderTotal_SumsTicketPrices() =>
        Assert.Equal(24.00m, BookingRules.OrderTotal([9.60m, 14.40m]));

    [Theory]
    [InlineData(null, null, SeatStatus.Free)]
    [InlineData(TicketStatus.Reserved, 1, SeatStatus.Held)]
    [InlineData(TicketStatus.Reserved, -1, SeatStatus.Free)]
    [InlineData(TicketStatus.Paid, null, SeatStatus.Sold)]
    [InlineData(TicketStatus.Cancelled, null, SeatStatus.Free)]
    public void StatusOf_FollowsTicketState(TicketStatus? status, int? reservedUntilMinutes, SeatStatus expected)
    {
        DateTime? until = reservedUntilMinutes is { } m ? Now.AddMinutes(m) : null;
        Assert.Equal(expected, BookingRules.StatusOf(status, until, Now));
    }

    [Fact]
    public void CanCancel_ExactlyDeadlineIsAllowed() =>
        Assert.True(BookingRules.CanCancel(Now.AddHours(24), Now));

    [Fact]
    public void CanCancel_OneMinuteLateIsRejected() =>
        Assert.False(BookingRules.CanCancel(Now.AddHours(24).AddMinutes(-1), Now));

    [Theory]
    [InlineData(false, 0, 10, 0, 0, false)] // inactive
    [InlineData(true, 1, 10, 0, 0, false)]  // not started
    [InlineData(true, -10, -1, 0, 0, false)] // expired
    [InlineData(true, -1, 10, 5, 5, false)] // limit reached
    [InlineData(true, -1, 10, 5, 4, true)]  // valid
    public void IsPromoUsable_ChecksAllConditions(bool active, int fromDays, int untilDays, int limit, int used, bool expected)
    {
        var promo = new PromoCode
        {
            IsActive = active,
            ValidFrom = Now.AddDays(fromDays),
            ValidUntil = Now.AddDays(untilDays),
            UsageLimit = limit,
            TimesUsed = used
        };
        Assert.Equal(expected, BookingRules.IsPromoUsable(promo, Now));
    }

    [Theory]
    [InlineData(120, 120, false)] // back-to-back
    [InlineData(60, 120, true)]   // start inside other
    public void Overlaps_DetectsIntersection(int offsetMinutes, int durationMinutes, bool expected) =>
        Assert.Equal(expected,
            BookingRules.Overlaps(Now, durationMinutes, Now.AddMinutes(offsetMinutes), 90));
}
