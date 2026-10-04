using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class EventsController(EventService eventService, PosterStorage posters) : AdminController
{
    public async Task<IActionResult> Index(string? search, EventType? type, string? genre, int page = 1)
    {
        var filter = new EventFilter(search, type, genre, Sort: EventSort.Title);
        return View(new AdminEventsVM(await eventService.SearchAsync(filter, page, 10),
            await eventService.GetFilterOptionsAsync(), search, type, genre));
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await eventService.GetEventAsync(id);
        if (ev is null)
            return NotFound();
        return View(new AdminEventDetailsVM(ev, await eventService.GetEventOccupancyAsync(id)));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormVM());

    [HttpPost]
    public async Task<IActionResult> Create(EventFormVM vm)
    {
        if (!await TrySavePosterAsync(vm))
            return View("Form", vm);
        var ev = await eventService.CreateEventAsync(vm.ToEntity());
        TempData["Success"] = "Събитието е създадено.";
        return RedirectToAction(nameof(Details), new { id = ev.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await eventService.GetEventAsync(id);
        return ev is null ? NotFound() : View("Form", EventFormVM.From(ev));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormVM vm)
    {
        vm.Id = id;
        var oldPoster = vm.PosterUrl;
        if (!await TrySavePosterAsync(vm))
            return View("Form", vm);
        try
        {
            await eventService.UpdateEventAsync(vm.ToEntity());
        }
        catch (BookingException ex)
        {
            if (vm.PosterUrl != oldPoster)
                posters.Delete(vm.PosterUrl);
            ModelState.AddModelError("", ex.Message);
            return View("Form", vm);
        }
        if (vm.PosterUrl != oldPoster)
            posters.Delete(oldPoster);
        TempData["Success"] = "Събитието е обновено.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Validates the form, stores an uploaded poster and points vm.PosterUrl at it.
    private async Task<bool> TrySavePosterAsync(EventFormVM vm)
    {
        if (!ModelState.IsValid)
            return false;
        if (vm.Poster is null)
            return true;
        try
        {
            vm.PosterUrl = await posters.SaveAsync(vm.Poster);
            return true;
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError(nameof(vm.Poster), ex.Message);
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await eventService.GetEventAsync(id);
        return ev is null ? NotFound() : View("Delete", new DeleteVM(id, "Изтриване на събитие", ev.Title));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var poster = (await eventService.GetEventAsync(id))?.PosterUrl;
            await eventService.DeleteEventAsync(id);
            posters.Delete(poster);
            TempData["Success"] = "Събитието е изтрито.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
