using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public record CartLineDto(
    int TicketId, int ShowtimeId, EventType EventType, string EventTitle, string VenueName, DateTime StartsAt, int Row, int Number,
    SeatCategory Category, ConcessionType Concession, decimal Fare, decimal Price, DateTime ReservedUntil);

public record ExpiredSeatDto(int ShowtimeId, string EventTitle, DateTime StartsAt, int SeatId, int Row, int Number);

public record CartDto(
    int OrderId, IReadOnlyList<CartLineDto> Lines, string? PromoCode, int PromoPercent,
    decimal Total, DateTime? EarliestExpiry, IReadOnlyList<ExpiredSeatDto> Expired)
{
    public int ExpiredCount => Expired.Count;

    // Fares before the promo, and what the promo takes off them. Line prices are rounded one by one,
    // so the discount is the difference rather than a percentage of the subtotal.
    public decimal Subtotal => Lines.Sum(l => l.Fare);
    public decimal PromoDiscount => Subtotal - Total;

    // Seconds until the soonest hold in the cart ends; 0 for an empty cart.
    public int SecondsLeft(DateTime now) => EarliestExpiry is { } expiry ? BookingRules.SecondsLeft(expiry, now) : 0;
}
