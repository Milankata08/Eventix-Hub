using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public enum SeatStatus { Free, Held, Sold }

public static class BookingRules
{
    public const int HoldMinutes = 10;
    public const int CancellationDeadlineHours = 24;
    public const int MaxTicketsPerOrder = 10;
    public const int StudentPercent = 20;
    public const int PensionerPercent = 30;
    public const int CriticalOccupancyPercent = 90;

    public static decimal TicketPrice(decimal standardPrice, decimal vipPrice, SeatCategory category,
        ConcessionType concession, int promoPercent)
    {
        var basePrice = category == SeatCategory.Vip ? vipPrice : standardPrice;
        var concessionPercent = concession switch
        {
            ConcessionType.Student => StudentPercent,
            ConcessionType.Pensioner => PensionerPercent,
            _ => 0
        };
        var price = basePrice * (100 - concessionPercent) / 100m * (100 - promoPercent) / 100m;
        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal OrderTotal(IEnumerable<decimal> ticketPrices) => ticketPrices.Sum();

    // status = null means no ticket exists for the seat.
    public static SeatStatus StatusOf(TicketStatus? status, DateTime? reservedUntil, DateTime now) => status switch
    {
        TicketStatus.Paid => SeatStatus.Sold,
        TicketStatus.Reserved when reservedUntil > now => SeatStatus.Held,
        _ => SeatStatus.Free
    };

    public static bool CanCancel(DateTime startsAt, DateTime now) =>
        startsAt - now >= TimeSpan.FromHours(CancellationDeadlineHours);

    public static bool IsPromoUsable(PromoCode promo, DateTime now) =>
        promo.IsActive && promo.ValidFrom <= now && now <= promo.ValidUntil && promo.TimesUsed < promo.UsageLimit;

    // Half-open intervals: back-to-back showtimes do not overlap.
    public static bool Overlaps(DateTime startA, int durationMinutesA, DateTime startB, int durationMinutesB) =>
        startA < startB.AddMinutes(durationMinutesB) && startB < startA.AddMinutes(durationMinutesA);
}
