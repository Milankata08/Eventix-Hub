using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

[Authorize]
public class TicketsController(TicketService ticketService) : Controller
{
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await ticketService.GetUserTicketAsync(User.GetUserId(), id);
        if (ticket is null)
            return NotFound();
        return View(new TicketCardVM(ticket, ticket.Status == Models.TicketStatus.Paid ? Qr.DataUri(ticket.Code) : null));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await ticketService.CancelTicketAsync(User.GetUserId(), id);
            TempData["Success"] = "Билетът е отказан.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Index", "Profile");
    }
}
