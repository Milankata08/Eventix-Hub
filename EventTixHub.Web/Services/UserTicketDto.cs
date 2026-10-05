using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public enum TicketHistoryFilter { Upcoming, Past, Cancelled }

public record UserTicketDto(
    int TicketId, string EventTitle, string VenueName, DateTime StartsAt, int Row, int Number,
    SeatCategory Category, ConcessionType Concession, decimal Price, string? Code,
    TicketStatus Status)
{
    public bool CanCancel { get; init; }
}
