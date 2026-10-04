using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class EventsController(EventService eventService, PosterStorage posters) : AdminController
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
        if (!ModelState.IsValid || !await TryUploadPosterAsync(vm))
            return View("Form", vm);
        try
        {
            var ev = await eventService.CreateEventAsync(vm.ToEntity());
            TempData["Success"] = "Събитието е създадено.";
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }
        catch
        {
            DiscardUploadedPoster(vm);
            throw;
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
        vm.PosterUrl = current.PosterUrl; // server-side value, never trusted from the form
        if (!ModelState.IsValid || !await TryUploadPosterAsync(vm))
            return View("Form", vm);
        if (vm.Poster is null && vm.RemovePoster)
            vm.PosterUrl = null;
        string? oldPoster;
        try
        {
            oldPoster = await eventService.UpdateEventAsync(vm.ToEntity());
        }
        catch (BookingException ex)
        {
            DiscardUploadedPoster(vm);
            vm.PosterUrl = current.PosterUrl;
            ModelState.AddModelError("", ex.Message);
            return View("Form", vm);
        }
        catch
        {
            DiscardUploadedPoster(vm);
            throw;
        }
        if (oldPoster != vm.PosterUrl)
            posters.Delete(oldPoster);
        TempData["Success"] = "Събитието е обновено.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // Rolls back a poster uploaded for a save that failed.
    private void DiscardUploadedPoster(EventFormVM vm)
    {
        if (vm.Poster is not null)
            posters.Delete(vm.PosterUrl);
    }

    // Stores an uploaded poster and points vm.PosterUrl at it.
    private async Task<bool> TryUploadPosterAsync(EventFormVM vm)
    {
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
            posters.Delete(await eventService.DeleteEventAsync(id));
            TempData["Success"] = "Събитието е изтрито.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
