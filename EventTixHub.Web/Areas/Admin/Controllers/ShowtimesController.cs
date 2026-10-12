using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class ShowtimesController(
    EventService eventService, VenueService venueService, TicketService ticketService, TimeProvider clock) : AdminController
{
    public async Task<IActionResult> Index(int? eventId, int? venueId, DateTime? from, DateTime? to, bool past = false, int page = 1)
    {
        var filter = new ShowtimeFilter(eventId, venueId, from, to, past);
        return View(new AdminShowtimesVM(await eventService.SearchShowtimesAsync(filter, page),
            await eventService.GetEventOptionsAsync(), await venueService.GetAllAsync(), filter));
    }

    // Live seat map (monitor mode polls the seats API).
    public async Task<IActionResult> Details(int id)
    {
        var map = await ticketService.GetSeatMapAsync(id);
        return map is null ? NotFound() : View(new HallVM(map, Monitor: true, SalesClosed: false, Hold: null));
    }

    [HttpGet]
    public async Task<IActionResult> Create() =>
        View("Form", await WithOptions(new ShowtimeFormVM { StartsAt = clock.GetLocalNow().DateTime.Date.AddDays(1).AddHours(19) }));

    [HttpPost]
    public async Task<IActionResult> Create(ShowtimeFormVM vm)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await eventService.CreateShowtimeAsync(vm.ToEntity());
                TempData["Success"] = "Прожекцията е създадена.";
                return RedirectToAction(nameof(Index));
            }
            catch (BookingException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        return View("Form", await WithOptions(vm));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var showtime = await eventService.GetShowtimeAsync(id);
        return showtime is null ? NotFound() : View("Form", await WithOptions(ShowtimeFormVM.From(showtime)));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ShowtimeFormVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await eventService.UpdateShowtimeAsync(vm.ToEntity());
                TempData["Success"] = "Прожекцията е обновена.";
                return RedirectToAction(nameof(Index));
            }
            catch (BookingException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        return View("Form", await WithOptions(vm));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var showtime = await eventService.GetShowtimeAsync(id);
        return showtime is null ? NotFound()
            : View("Delete", new DeleteVM(id, "Изтриване на прожекция",
                $"{showtime.Event.Title} · {showtime.Venue.Name} · {showtime.StartsAt:dd.MM.yyyy HH:mm}"));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await eventService.DeleteShowtimeAsync(id);
            TempData["Success"] = "Прожекцията е изтрита.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<ShowtimeFormVM> WithOptions(ShowtimeFormVM vm)
    {
        vm.Events = await eventService.GetEventOptionsAsync();
        vm.Venues = (await venueService.GetAllAsync()).Select(v => new VenueOption(v.Id, $"{v.Name} ({v.City})")).ToList();
        return vm;
    }
}
