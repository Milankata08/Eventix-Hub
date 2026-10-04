using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class PromoCodesController(PromoCodeService promoService, TimeProvider clock) : AdminController
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    public async Task<IActionResult> Index(string? search, bool? active, int page = 1) =>
        View(new AdminPromoCodesVM(await promoService.SearchAsync(search, active, page), search, active, Now));

    [HttpGet]
    public IActionResult Create() =>
        View("Form", new PromoCodeFormVM { ValidFrom = Now.Date, ValidUntil = Now.Date.AddMonths(1) });

    [HttpPost]
    public async Task<IActionResult> Create(PromoCodeFormVM vm)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await promoService.CreateAsync(vm.ToEntity());
                TempData["Success"] = "Промо кодът е създаден.";
                return RedirectToAction(nameof(Index));
            }
            catch (BookingException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        return View("Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var promo = await promoService.GetByIdAsync(id);
        return promo is null ? NotFound() : View("Form", PromoCodeFormVM.From(promo));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PromoCodeFormVM vm)
    {
        vm.Id = id;
        if (ModelState.IsValid)
        {
            try
            {
                await promoService.UpdateAsync(vm.ToEntity());
                TempData["Success"] = "Промо кодът е обновен.";
                return RedirectToAction(nameof(Index));
            }
            catch (BookingException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }
        return View("Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await promoService.GetByIdAsync(id);
        return promo is null ? NotFound() : View("Delete", new DeleteVM(id, "Изтриване на промо код", promo.Code));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await promoService.DeleteAsync(id);
            TempData["Success"] = "Промо кодът е изтрит.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
