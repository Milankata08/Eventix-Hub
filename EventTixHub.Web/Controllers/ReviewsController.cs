using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

[Authorize]
public class ReviewsController(ReviewService reviewService) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Add(ReviewVM vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToDetails(vm.EventId);
        }
        try
        {
            await reviewService.AddAsync(User.GetUserId(), vm.EventId, vm.Rating, vm.Comment);
            TempData["Success"] = "Оценката е добавена.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToDetails(vm.EventId);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        try
        {
            await reviewService.DeleteAsync(id, User.GetUserId(), User.IsInRole(Roles.Administrator));
            TempData["Success"] = "Оценката е изтрита.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToDetails(eventId);
    }

    private RedirectToActionResult RedirectToDetails(int eventId) =>
        RedirectToAction("Details", "Events", new { id = eventId });
}
