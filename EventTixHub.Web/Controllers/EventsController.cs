using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

public class EventsController(EventService eventService, ReviewService reviewService) : Controller
{
    public async Task<IActionResult> Details(int id, int page = 1)
    {
        var details = await eventService.GetDetailsAsync(id);
        if (details is null)
            return NotFound();
        var loggedIn = User.Identity?.IsAuthenticated == true;
        var userId = loggedIn ? User.GetUserId() : (int?)null;
        return View(new EventDetailsVM
        {
            Details = details,
            Reviews = await reviewService.GetForEventAsync(id, page),
            CanReview = userId is { } uid && await reviewService.CanReviewAsync(uid, id),
            CurrentUserId = userId,
            IsAdmin = User.IsInRole(Roles.Administrator)
        });
    }
}
