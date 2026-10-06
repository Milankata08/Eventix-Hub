using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class EventsController(EventService eventService) : AdminController
{
    // Poster limit plus headroom for the other form fields.
    private const int MaxRequestBytes = (int)PosterStorage.MaxBytes + 1024 * 1024;

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

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(EventFormVM vm)
    {
        if (!ModelState.IsValid)
            return View("Form", vm);
        try
        {
            var ev = await eventService.CreateEventAsync(vm.ToEntity(), vm.Poster);
            TempData["Success"] = "Събитието е създадено.";
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }
        catch (PosterException ex)
        {
            ModelState.AddModelError(nameof(vm.Poster), ex.Message);
            return View("Form", vm);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await eventService.GetEventAsync(id);
        return ev is null ? NotFound() : View("Form", EventFormVM.From(ev));
    }

    [HttpPost, RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Edit(int id, EventFormVM vm)
    {
        var current = await eventService.GetEventAsync(id);
        if (current is null)
            return NotFound();
        vm.Id = id;
        vm.PosterUrl = current.PosterUrl; // display only: the service ignores it
        if (!ModelState.IsValid)
            return View("Form", vm);
        try
        {
            await eventService.UpdateEventAsync(vm.ToEntity(), vm.Poster, vm.RemovePoster);
        }
        catch (PosterException ex)
        {
            ModelState.AddModelError(nameof(vm.Poster), ex.Message);
            return View("Form", vm);
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Form", vm);
        }
        TempData["Success"] = "Събитието е обновено.";
        return RedirectToAction(nameof(Details), new { id });
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
            await eventService.DeleteEventAsync(id);
            TempData["Success"] = "Събитието е изтрито.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
