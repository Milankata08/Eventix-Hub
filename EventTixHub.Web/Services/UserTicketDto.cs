using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public enum TicketHistoryFilter { Upcoming, Past, Cancelled }

public record UserTicketDto(
    int TicketId, EventType EventType, string EventTitle, string VenueName, string City, string VenueAddress, DateTime StartsAt, int Row, int Number,
    SeatCategory Category, ConcessionType Concession, decimal Price, string? Code,
    TicketStatus Status)
{
    public bool CanCancel { get; init; }
}
