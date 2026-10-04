using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public record CartLineDto(
    int TicketId, string EventTitle, string VenueName, DateTime StartsAt, int Row, int Number,
    SeatCategory Category, ConcessionType Concession, decimal Price, DateTime ReservedUntil);

public record CartDto(
    int OrderId, IReadOnlyList<CartLineDto> Lines, string? PromoCode, int PromoPercent,
    decimal Total, DateTime? EarliestExpiry, int ExpiredCount);
