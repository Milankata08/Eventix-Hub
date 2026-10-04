using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class VenuesController(VenueService venueService) : AdminController
{
    public async Task<IActionResult> Index(string? search, string? city, int page = 1) =>
        View(new AdminVenuesVM(await venueService.SearchAsync(search, city, page),
            await venueService.GetCitiesAsync(), search, city));

    public async Task<IActionResult> Details(int id)
    {
        var venue = await venueService.GetByIdAsync(id);
        return venue is null ? NotFound() : View(new VenueDetailsVM(venue, await venueService.GetSeatsAsync(id)));
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new VenueFormVM());

    [HttpPost]
    public async Task<IActionResult> Create(VenueFormVM vm)
    {
        if (!ModelState.IsValid)
            return View("Form", vm);
        var venue = await venueService.CreateAsync(vm.ToEntity());
        TempData["Success"] = "Залата е създадена.";
        return RedirectToAction(nameof(Details), new { id = venue.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var venue = await venueService.GetByIdAsync(id);
        return venue is null ? NotFound() : View("Form", VenueFormVM.From(venue));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VenueFormVM vm)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
            return View("Form", vm);
        try
        {
            await venueService.UpdateAsync(vm.ToEntity());
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View("Form", vm);
        }
        TempData["Success"] = "Залата е обновена.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleRowVip(int id, int row)
    {
        try
        {
            await venueService.ToggleRowVipAsync(id, row);
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var venue = await venueService.GetByIdAsync(id);
        return venue is null ? NotFound() : View("Delete", new DeleteVM(id, "Изтриване на зала", $"{venue.Name}, {venue.City}"));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await venueService.DeleteAsync(id);
            TempData["Success"] = "Залата е изтрита.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
