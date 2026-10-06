using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

// Occupancy of one showtime (PLAN §4). Taken = sold + live holds; critical at >= CriticalOccupancyPercent.
public record ShowtimeRowDto(
    int Id, int EventId, string EventTitle, string VenueName, DateTime StartsAt,
    int Capacity, int Sold, int Held, EventType Type = EventType.Movie)
{
    public int Taken => Sold + Held;
    public int Free => Capacity - Taken;
    public double Percent => Capacity == 0 ? 0 : 100.0 * Taken / Capacity;
    public bool IsCritical => Percent >= BookingRules.CriticalOccupancyPercent;
}

public static class OccupancyQueries
{
    // Callers filter and order the showtimes first; this only projects.
    public static IQueryable<ShowtimeRowDto> ToOccupancyRows(this IQueryable<Showtime> query, DateTime now)
    {
        var held = BookingRules.LiveHold(now);
        return query.Select(s => new ShowtimeRowDto(s.Id, s.EventId, s.Event.Title, s.Venue.Name, s.StartsAt,
            s.Venue.Rows * s.Venue.SeatsPerRow,
            s.Tickets.Count(t => t.Status == TicketStatus.Paid),
            s.Tickets.AsQueryable().Count(held), s.Event.Type));
    }
}
