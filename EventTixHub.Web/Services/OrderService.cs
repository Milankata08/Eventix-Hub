using System.Security.Cryptography;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class OrderService(
    IRepository<Order> orders,
    IRepository<Ticket> tickets,
    IRepository<PromoCode> promos,
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
        .Where(t => t.Status == TicketStatus.Reserved && t.ReservedUntil > now)
        .OrderBy(t => t.Id).ToList();

    private static int PromoPercent(Order cart, DateTime now) =>
        cart.PromoCode is { } p && BookingRules.IsPromoUsable(p, now) ? p.DiscountPercent : 0;

    private static void Reprice(Order cart, DateTime now)
    {
        var percent = PromoPercent(cart, now);
        foreach (var t in ActiveTickets(cart, now))
            t.Price = BookingRules.TicketPrice(t.Showtime.StandardPrice, t.Showtime.VipPrice, t.Seat.Category,
                t.Concession, percent);
    }

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

    public async Task<CartDto?> GetCartAsync(int userId)
    {
        var cart = await LoadCartAsync(userId);
        if (cart is null)
            return null;

        var now = Now;
        var active = ActiveTickets(cart, now);
        var percent = PromoPercent(cart, now);
        var lines = active.Select(t => new CartLineDto(t.Id, t.Showtime.Event.Title, t.Showtime.Venue.Name,
            t.Showtime.StartsAt, t.Seat.Row, t.Seat.Number, t.Seat.Category, t.Concession,
            BookingRules.TicketPrice(t.Showtime.StandardPrice, t.Showtime.VipPrice, t.Seat.Category, t.Concession,
                percent),
            t.ReservedUntil!.Value)).ToList();
        return new CartDto(cart.Id, lines, percent > 0 ? cart.PromoCode!.Code : null, percent,
            BookingRules.OrderTotal(lines.Select(l => l.Price)),
            lines.Count == 0 ? null : lines.Min(l => l.ReservedUntil));
    }

    public async Task SetConcessionAsync(int userId, int ticketId, ConcessionType concession)
    {
        var now = Now;
        var cart = await LoadCartAsync(userId);
        var ticket = cart is null ? null : ActiveTickets(cart, now).FirstOrDefault(t => t.Id == ticketId);
        if (cart is null || ticket is null)
            throw new BookingException("Билетът не е намерен в количката.");

        ticket.Concession = concession;
        Reprice(cart, now);
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
        Reprice(cart, now);
        await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
    }

    public async Task RemovePromoAsync(int userId)
    {
        var cart = await LoadCartAsync(userId)
            ?? throw new BookingException("Количката е празна.");
        cart.PromoCodeId = null;
        cart.PromoCode = null;
        Reprice(cart, Now);
        await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
    }

    // BR-09, BR-10
    public async Task<Order> CheckoutAsync(int userId)
    {
        var now = Now;
        var cart = await LoadCartAsync(userId)
            ?? throw new BookingException("Количката е празна.");

        var expired = cart.Tickets.Where(t => t.Status == TicketStatus.Reserved && t.ReservedUntil <= now).ToList();
        if (expired.Count > 0)
        {
            foreach (var t in expired)
                tickets.Remove(t);
            await SaveAsync("Количката току-що беше променена. Опреснете страницата.");
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

        Reprice(cart, now);
        foreach (var t in active)
        {
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

    public Task<Order?> GetUserOrderAsync(int userId, int orderId) => orders.Query()
        .Include(o => o.Tickets).ThenInclude(t => t.Seat)
        .Include(o => o.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Event)
        .Include(o => o.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Venue)
        .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
}
