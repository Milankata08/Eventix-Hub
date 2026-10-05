using System.Security.Cryptography;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class OrderService(
    IRepository<Order> orders,
    IRepository<Ticket> tickets,
    IRepository<PromoCode> promos,
    IRepository<Showtime> showtimes,
    IRepository<Seat> seats,
    TimeProvider clock,
    ILogger<OrderService> logger)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    // The user's Pending order with its tickets (seat + showtime loaded) and promo; null when there is no cart.
    private Task<Order?> LoadCartAsync(int userId) => orders.Query()
        .Include(o => o.PromoCode)
        .Include(o => o.Tickets).ThenInclude(t => t.Seat)
        .Include(o => o.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Event)
        .Include(o => o.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Venue)
        .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending);

    private static List<Ticket> ActiveTickets(Order cart, DateTime now) => cart.Tickets
        .AsQueryable().Where(BookingRules.LiveHold(now))
        .OrderBy(t => t.Id).ToList();

    private static int PromoPercent(Order cart, DateTime now) =>
        cart.PromoCode is { } p && BookingRules.IsPromoUsable(p, now) ? p.DiscountPercent : 0;

    private static decimal PriceOf(Ticket t, int promoPercent) =>
        BookingRules.TicketPrice(t.Showtime.StandardPrice, t.Showtime.VipPrice, t.Seat.Category, t.Concession,
            promoPercent);

    private async Task SaveAsync(string conflictMessage)
    {
        try
        {
            await orders.SaveChangesAsync();
        }
        catch (DbUpdateException ex) // includes DbUpdateConcurrencyException
        {
            logger.LogWarning(ex, "Order conflict: {Message}", conflictMessage);
            throw new BookingException(conflictMessage);
        }
    }

    // BR-03: expired holds are deleted with their own SaveChanges before anything else is saved.
    private async Task DropHoldsAsync(IReadOnlyCollection<Ticket> holds, string conflictMessage)
    {
        foreach (var t in holds)
            tickets.Remove(t);
        await SaveAsync(conflictMessage);
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
        var inCart = cart is null ? 0 : await tickets.Query()
            .Where(BookingRules.LiveHold(now)).CountAsync(t => t.OrderId == cart.Id);
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

        // The user's own expired holds are dead cart weight: drop them along with the requested seats' ones.
        if (cart is not null)
            expired.AddRange((await tickets.Query()
                .Where(t => t.OrderId == cart.Id).Where(BookingRules.ExpiredHold(now))
                .ToListAsync()).Except(expired));

        if (expired.Count > 0)
            await DropHoldsAsync(expired, "Някое от местата току-що беше заето. Опитайте отново.");

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
                ReservedUntil = now.AddMinutes(BookingRules.HoldMinutes)
            };
            await tickets.AddAsync(ticket);
        }

        // Filtered unique index: a concurrent request won the seat.
        await SaveAsync("Някое от местата току-що беше заето. Опитайте отново.");
        return cart;
    }

    public async Task RemoveFromCartAsync(int userId, int ticketId)
    {
        var ticket = await tickets.Query().FirstOrDefaultAsync(t =>
            t.Id == ticketId && t.Status == TicketStatus.Reserved
            && t.Order.UserId == userId && t.Order.Status == OrderStatus.Pending)
            ?? throw new BookingException("Билетът не е намерен в количката.");

        tickets.Remove(ticket);
        await SaveAsync("Билетът току-що беше променен. Опреснете количката.");
    }

    public async Task<CartDto?> GetCartAsync(int userId)
    {
        var cart = await LoadCartAsync(userId);
        if (cart is null)
            return null;

        var now = Now;
        var active = ActiveTickets(cart, now);
        var percent = PromoPercent(cart, now);
        var lines = active.Select(t => new CartLineDto(t.Id, t.Showtime.Event.Title, t.Showtime.Venue.Name,
            t.Showtime.StartsAt, t.Seat.Row, t.Seat.Number, t.Seat.Category, t.Concession, PriceOf(t, percent),
            t.ReservedUntil!.Value)).ToList();
        return new CartDto(cart.Id, lines, percent > 0 ? cart.PromoCode!.Code : null, percent,
            BookingRules.OrderTotal(lines.Select(l => l.Price)),
            lines.Count == 0 ? null : lines.Min(l => l.ReservedUntil),
            cart.Tickets.AsQueryable().Count(BookingRules.ExpiredHold(now)));
    }

    public async Task SetConcessionAsync(int userId, int ticketId, ConcessionType concession)
    {
        var now = Now;
        var cart = await LoadCartAsync(userId);
        var ticket = cart is null ? null : ActiveTickets(cart, now).FirstOrDefault(t => t.Id == ticketId);
        if (cart is null || ticket is null)
            throw new BookingException("Билетът не е намерен в количката.");

        ticket.Concession = concession;
        await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
    }

    // BR-08
    public async Task ApplyPromoAsync(int userId, string code)
    {
        var now = Now;
        var cart = await LoadCartAsync(userId)
            ?? throw new BookingException("Количката е празна.");
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        var promo = await promos.Query().FirstOrDefaultAsync(p => p.Code == normalized);
        if (promo is null || !BookingRules.IsPromoUsable(promo, now))
            throw new BookingException("Промо кодът е невалиден или изчерпан.");

        cart.PromoCodeId = promo.Id;
        cart.PromoCode = promo;
        await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
    }

    public async Task RemovePromoAsync(int userId)
    {
        var cart = await LoadCartAsync(userId)
            ?? throw new BookingException("Количката е празна.");
        cart.PromoCodeId = null;
        cart.PromoCode = null;
        await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
    }

    // BR-09, BR-10
    public async Task<Order> CheckoutAsync(int userId)
    {
        var now = Now;
        var cart = await LoadCartAsync(userId)
            ?? throw new BookingException("Количката е празна.");

        var expired = cart.Tickets.AsQueryable().Where(BookingRules.ExpiredHold(now)).ToList();
        if (expired.Count > 0)
        {
            await DropHoldsAsync(expired, "Количката току-що беше променена. Опреснете страницата.");
            throw new BookingException("Резервацията изтече. Изберете местата отново.");
        }

        var active = ActiveTickets(cart, now);
        if (active.Count == 0)
            throw new BookingException("Количката е празна.");
        if (active.Any(t => now >= t.Showtime.StartsAt))
            throw new BookingException("Продажбата на билети за тази прожекция е приключила.");

        var promo = cart.PromoCode;
        if (promo is not null && !BookingRules.IsPromoUsable(promo, now))
            throw new BookingException("Промо кодът е невалиден или изчерпан.");

        var percent = PromoPercent(cart, now);
        foreach (var t in active)
        {
            t.Price = PriceOf(t, percent);
            t.Status = TicketStatus.Paid;
            t.ReservedUntil = null;
            t.Code = "ETH-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(5));
        }
        if (promo is not null)
            promo.TimesUsed++;
        cart.Status = OrderStatus.Paid;
        cart.PaidAt = now;
        cart.TotalAmount = BookingRules.OrderTotal(active.Select(t => t.Price));

        await SaveAsync("Плащането не успя, защото количката се промени. Опитайте отново.");
        return cart;
    }

    public Task<Order?> GetUserOrderAsync(int userId, int orderId) =>
        orders.Query().FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
}
