using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class EventsController(EventService eventService) : AdminController
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
        if (!ModelState.IsValid)
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
        if (!ModelState.IsValid)
            return View("Form", vm);
        try
        {
            await eventService.UpdateEventAsync(vm.ToEntity());
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
