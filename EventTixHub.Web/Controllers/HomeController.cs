using System.Diagnostics;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

public class HomeController(EventService eventService) : Controller
{
    public async Task<IActionResult> Index([FromQuery] EventFilter filter, int page = 1) =>
        View(new AfishaVM
        {
            Filter = filter,
            Options = await eventService.GetFilterOptionsAsync(),
            Events = await eventService.SearchAsync(filter, page)
        });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int id) => View(id);
}
