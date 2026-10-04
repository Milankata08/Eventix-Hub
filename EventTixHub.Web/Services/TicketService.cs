using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class TicketService(
    IRepository<Showtime> showtimes,
    IRepository<Seat> seats,
    IRepository<Ticket> tickets,
    IRepository<Order> orders,
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
                EventTitle = s.Event.Title, VenueName = s.Venue.Name
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

        return new SeatMapDto(showtime.Id, showtime.EventTitle, showtime.VenueName, showtime.StartsAt,
            showtime.StandardPrice, showtime.VipPrice, map,
            map.Count(s => s.Status == SeatStatus.Free),
            map.Count(s => s.Status == SeatStatus.Held),
            map.Count(s => s.Status == SeatStatus.Sold));
    }

    // BR-02..05. Returns the user's cart (Pending order).
    public async Task<Order> ReserveSeatsAsync(int userId, int showtimeId, IReadOnlyCollection<int> seatIds)
    {
        var now = Now;
        var showtime = await showtimes.GetByIdAsync(showtimeId)
            ?? throw new BookingException("Прожекцията не е намерена.");
        if (now >= showtime.StartsAt)
            throw new BookingException("Продажбата на билети за тази прожекция е приключила.");

        var ids = seatIds.Distinct().ToList();
        if (ids.Count == 0 || ids.Count != seatIds.Count)
            throw new BookingException("Изберете поне едно място, без повторения.");

        var chosen = await seats.Query().Where(s => ids.Contains(s.Id)).ToListAsync();
        if (chosen.Count != ids.Count || chosen.Any(s => s.VenueId != showtime.VenueId))
            throw new BookingException("Избрано е място, което не е от залата на прожекцията.");

        var cart = await orders.Query()
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending);
        var inCart = cart is null ? 0 : await tickets.Query().CountAsync(t =>
            t.OrderId == cart.Id && t.Status == TicketStatus.Reserved && t.ReservedUntil > now);
        if (inCart + ids.Count > BookingRules.MaxTicketsPerOrder)
            throw new BookingException($"В количката може да има най-много {BookingRules.MaxTicketsPerOrder} билета.");

        var existing = await tickets.Query()
            .Where(t => t.ShowtimeId == showtimeId && ids.Contains(t.SeatId) && t.Status != TicketStatus.Cancelled)
            .ToListAsync();
        var expired = new List<Ticket>();
        foreach (var t in existing)
        {
            if (BookingRules.StatusOf(t.Status, t.ReservedUntil, now) == SeatStatus.Free)
            {
                expired.Add(t);
                continue;
            }
            var seat = chosen.First(s => s.Id == t.SeatId);
            logger.LogWarning("Seat {SeatId} of showtime {ShowtimeId} already taken", seat.Id, showtimeId);
            throw new BookingException($"Ред {seat.Row}, място {seat.Number} е заето.");
        }

        if (expired.Count > 0)
        {
            foreach (var t in expired)
                tickets.Remove(t);
            try
            {
                await tickets.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Expired hold changed concurrently on showtime {ShowtimeId}", showtimeId);
                throw new BookingException("Някое от местата току-що беше заето. Опитайте отново.");
            }
        }

        if (cart is null)
        {
            cart = new Order { UserId = userId, CreatedAt = now, Status = OrderStatus.Pending };
            await orders.AddAsync(cart);
        }
        foreach (var seat in chosen)
        {
            var ticket = new Ticket
            {
                Order = cart,
                ShowtimeId = showtimeId,
                SeatId = seat.Id,
                Status = TicketStatus.Reserved,
                Concession = ConcessionType.Regular,
                Price = BookingRules.TicketPrice(showtime.StandardPrice, showtime.VipPrice, seat.Category,
                    ConcessionType.Regular, 0),
                ReservedUntil = now.AddMinutes(BookingRules.HoldMinutes)
            };
            await tickets.AddAsync(ticket);
        }

        try
        {
            await tickets.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Filtered unique index: a concurrent request won the seat.
            logger.LogWarning(ex, "Booking conflict on showtime {ShowtimeId}", showtimeId);
            throw new BookingException("Някое от местата току-що беше заето. Опитайте отново.");
        }
        return cart;
    }

    public async Task RemoveFromCartAsync(int userId, int ticketId)
    {
        var ticket = await tickets.Query().FirstOrDefaultAsync(t =>
            t.Id == ticketId && t.Status == TicketStatus.Reserved
            && t.Order.UserId == userId && t.Order.Status == OrderStatus.Pending)
            ?? throw new BookingException("Билетът не е намерен в количката.");

        tickets.Remove(ticket);
        try
        {
            await tickets.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Ticket {TicketId} changed while being removed", ticketId);
            throw new BookingException("Билетът току-що беше променен. Опреснете количката.");
        }
    }
}
