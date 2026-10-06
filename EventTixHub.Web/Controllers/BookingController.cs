using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

[Authorize]
public class BookingController(TicketService ticketService, OrderService orderService, TimeProvider clock) : Controller
{
    public async Task<IActionResult> Seats(int id)
    {
        var map = await ticketService.GetSeatMapAsync(id);
        if (map is null)
            return NotFound();
        return View(new SeatsVM(map, map.StartsAt <= clock.GetLocalNow().DateTime));
    }

    [HttpPost]
    public async Task<IActionResult> Reserve(int showtimeId, int[]? seatIds)
    {
        try
        {
            await orderService.ReserveSeatsAsync(User.GetUserId(), showtimeId, seatIds ?? []);
            return RedirectToAction(nameof(Cart));
        }
        catch (BookingException ex)
        {
            // Reloading the seats page also refreshes the map.
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Seats), new { id = showtimeId });
        }
    }

    public async Task<IActionResult> Cart()
    {
        var cart = await orderService.GetCartAsync(User.GetUserId());
        return View(new CartVM(cart, SecondsLeft(cart)));
    }

    [HttpPost]
    public Task<IActionResult> SetConcession(int ticketId, ConcessionType concession) =>
        CartAction(() => orderService.SetConcessionAsync(User.GetUserId(), ticketId, concession));

    [HttpPost]
    public Task<IActionResult> ApplyPromo(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Въведете промо код.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Cart)));
        }
        return CartAction(() => orderService.ApplyPromoAsync(User.GetUserId(), code));
    }

    [HttpPost]
    public Task<IActionResult> RemovePromo() =>
        CartAction(() => orderService.RemovePromoAsync(User.GetUserId()));

    [HttpPost]
    public Task<IActionResult> RemoveTicket(int ticketId) =>
        CartAction(() => orderService.RemoveFromCartAsync(User.GetUserId(), ticketId));

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await orderService.GetCartAsync(User.GetUserId());
        if (cart is null || cart.Lines.Count == 0)
            return RedirectToAction(nameof(Cart));
        return View(new CheckoutVM(cart, new PaymentVM()));
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([Bind(Prefix = "Payment")] PaymentVM payment)
    {
        var userId = User.GetUserId();
        if (ModelState.IsValid)
        {
            try
            {
                var order = await orderService.CheckoutAsync(userId);
                return RedirectToAction(nameof(Confirmation), new { id = order.Id });
            }
            catch (BookingException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        var cart = await orderService.GetCartAsync(userId);
        if (cart is null || cart.Lines.Count == 0)
        {
            TempData["Error"] = "Резервацията изтече или количката е празна.";
            return RedirectToAction(nameof(Cart));
        }
        // Card data is never echoed back.
        return View(new CheckoutVM(cart, new PaymentVM { CardholderName = payment.CardholderName }));
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var userId = User.GetUserId();
        var order = await orderService.GetUserOrderAsync(userId, id);
        if (order is null)
            return NotFound();
        if (order.Status == OrderStatus.Pending)
            return RedirectToAction(nameof(Cart));

        var cards = (await ticketService.GetOrderTicketsAsync(userId, id))
            .Select(t => new TicketCardVM(t, Qr.DataUri(t.Code)))
            .ToList();
        return View(new ConfirmationVM(order.Id, order.TotalAmount, cards));
    }

    private async Task<IActionResult> CartAction(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Cart));
    }

    private int SecondsLeft(CartDto? cart) => cart?.EarliestExpiry is { } expiry
        ? Math.Max(0, (int)(expiry - clock.GetLocalNow().DateTime).TotalSeconds)
        : 0;
}
