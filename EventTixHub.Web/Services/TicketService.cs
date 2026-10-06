using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class TicketService(
    IRepository<Showtime> showtimes,
    IRepository<Seat> seats,
    IRepository<Ticket> tickets,
    TimeProvider clock,
    ILogger<TicketService> logger)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    // BR-01
    public async Task<SeatMapDto?> GetSeatMapAsync(int showtimeId)
    {
        var showtime = await showtimes.Query()
            .Where(s => s.Id == showtimeId)
            .Select(s => new
            {
                s.Id, s.VenueId, s.StartsAt, s.StandardPrice, s.VipPrice,
                EventType = s.Event.Type, EventTitle = s.Event.Title, VenueName = s.Venue.Name
            })
            .FirstOrDefaultAsync();
        if (showtime is null)
            return null;

        var now = Now;
        var venueSeats = await seats.Query()
            .Where(s => s.VenueId == showtime.VenueId)
            .OrderBy(s => s.Row).ThenBy(s => s.Number)
            .ToListAsync();
        var taken = (await tickets.Query()
                .Where(t => t.ShowtimeId == showtimeId && t.Status != TicketStatus.Cancelled)
                .Select(t => new { t.SeatId, t.Status, t.ReservedUntil })
                .ToListAsync())
            .ToDictionary(t => t.SeatId);

        var map = venueSeats
            .Select(s => new SeatDto(s.Id, s.Row, s.Number, s.Category,
                taken.TryGetValue(s.Id, out var t)
                    ? BookingRules.StatusOf(t.Status, t.ReservedUntil, now)
                    : BookingRules.StatusOf(null, null, now)))
            .ToList();

        return new SeatMapDto(showtime.Id, showtime.EventType, showtime.EventTitle, showtime.VenueName, showtime.StartsAt,
            showtime.StandardPrice, showtime.VipPrice, map,
            map.Count(s => s.Status == SeatStatus.Free),
            map.Count(s => s.Status == SeatStatus.Held),
            map.Count(s => s.Status == SeatStatus.Sold));
    }

    // BR-11
    public async Task CancelTicketAsync(int userId, int ticketId)
    {
        var ticket = await tickets.Query()
            .Include(t => t.Order).Include(t => t.Showtime)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.Order.UserId == userId && t.Status == TicketStatus.Paid)
            ?? throw new BookingException("Билетът не е намерен.");

        var now = Now;
        if (!BookingRules.CanCancel(ticket.Showtime.StartsAt, now))
            throw new BookingException(
                $"Билет може да се откаже най-късно {BookingRules.CancellationDeadlineHours} часа преди началото.");

        ticket.Status = TicketStatus.Cancelled;
        ticket.CancelledAt = now;
        var othersActive = await tickets.Query().AnyAsync(t =>
            t.OrderId == ticket.OrderId && t.Id != ticket.Id && t.Status != TicketStatus.Cancelled);
        if (!othersActive)
            ticket.Order.Status = OrderStatus.Cancelled;

        try
        {
            await tickets.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Ticket {TicketId} changed while being cancelled", ticketId);
            throw new BookingException("Билетът току-що беше променен. Опреснете страницата.");
        }
    }

    public async Task<PagedResult<UserTicketDto>> GetUserTicketsAsync(
        int userId, TicketHistoryFilter filter, int page, int pageSize = 10)
    {
        var now = Now;
        var query = OwnedHistory(userId);
        query = filter switch
        {
            TicketHistoryFilter.Cancelled => query.Where(t => t.Status == TicketStatus.Cancelled),
            TicketHistoryFilter.Past => query.Where(t => t.Status == TicketStatus.Paid && t.Showtime.StartsAt < now),
            _ => query.Where(t => t.Status == TicketStatus.Paid && t.Showtime.StartsAt >= now)
        };
        // Upcoming: soonest first; otherwise most recent first.
        query = filter == TicketHistoryFilter.Upcoming
            ? query.OrderBy(t => t.Showtime.StartsAt)
            : query.OrderByDescending(t => t.Showtime.StartsAt);
        var result = await ToDto(query).ToPagedResultAsync(page, pageSize);
        return result with { Items = result.Items.Select(t => WithCanCancel(t, now)).ToList() };
    }

    public async Task<UserTicketDto?> GetUserTicketAsync(int userId, int ticketId)
    {
        var ticket = await ToDto(OwnedHistory(userId).Where(t => t.Id == ticketId)).FirstOrDefaultAsync();
        return ticket is null ? null : WithCanCancel(ticket, Now);
    }

    public async Task<IReadOnlyList<UserTicketDto>> GetOrderTicketsAsync(int userId, int orderId)
    {
        var now = Now;
        var list = await ToDto(OwnedHistory(userId)
            .Where(t => t.OrderId == orderId && t.Status == TicketStatus.Paid)
            .OrderBy(t => t.Id)).ToListAsync();
        return list.Select(t => WithCanCancel(t, now)).ToList();
    }

    private static UserTicketDto WithCanCancel(UserTicketDto t, DateTime now) =>
        t with { CanCancel = t.Status == TicketStatus.Paid && BookingRules.CanCancel(t.StartsAt, now) };

    // Paid and cancelled tickets only: reserved ones are cart items, not history.
    private IQueryable<Ticket> OwnedHistory(int userId) => tickets.Query()
        .Where(t => t.Order.UserId == userId && t.Status != TicketStatus.Reserved);

    private static IQueryable<UserTicketDto> ToDto(IQueryable<Ticket> query) => query
        .Select(t => new UserTicketDto(t.Id, t.Showtime.Event.Type, t.Showtime.Event.Title, t.Showtime.Venue.Name,
            t.Showtime.Venue.City, t.Showtime.Venue.Address, t.Showtime.StartsAt,
            t.Seat.Row, t.Seat.Number, t.Seat.Category, t.Concession, t.Price, t.Code, t.Status));
}
