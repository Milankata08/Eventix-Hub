using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

[Authorize]
public class ProfileController(UserService userService, TicketService ticketService) : Controller
{
    public async Task<IActionResult> Index(TicketHistoryFilter filter = TicketHistoryFilter.Upcoming, int page = 1)
    {
        var userId = User.GetUserId();
        var user = await userService.GetByIdAsync(userId);
        if (user is null)
            return NotFound();
        return View(new ProfileIndexVM
        {
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Filter = filter,
            Tickets = await ticketService.GetUserTicketsAsync(userId, filter, page)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await userService.GetByIdAsync(User.GetUserId());
        if (user is null)
            return NotFound();
        return View(new ProfileEditVM { FirstName = user.FirstName, LastName = user.LastName, PhoneNumber = user.PhoneNumber });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileEditVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        try
        {
            await userService.UpdateProfileAsync(User.GetUserId(), vm.FirstName, vm.LastName, vm.PhoneNumber);
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
        TempData["Success"] = "Профилът е обновен.";
        return RedirectToAction(nameof(Index));
    }
}
