using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class HomeController(ReportService reportService, EventService eventService) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var upcoming = await eventService.SearchShowtimesAsync(new ShowtimeFilter(), 1);
        return View(new AdminDashboardVM(await reportService.GetDashboardAsync(), upcoming.Items));
    }
}
