using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public record CartLineDto(
    int TicketId, int ShowtimeId, EventType EventType, string EventTitle, string VenueName, DateTime StartsAt, int Row, int Number,
    SeatCategory Category, ConcessionType Concession, decimal Price, DateTime ReservedUntil);

public record ExpiredSeatDto(int ShowtimeId, string EventTitle, DateTime StartsAt, int SeatId, int Row, int Number);

public record CartDto(
    int OrderId, IReadOnlyList<CartLineDto> Lines, string? PromoCode, int PromoPercent,
    decimal Total, DateTime? EarliestExpiry, IReadOnlyList<ExpiredSeatDto> Expired)
{
    public int ExpiredCount => Expired.Count;

    // Seats this cart holds for one showtime; the seat page names them so the buyer knows the "held" seats are theirs.
    public int SeatsFor(int showtimeId) => Lines.Count(l => l.ShowtimeId == showtimeId);
}
