using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class ReportsController(ReportService reportService, TimeProvider clock) : AdminController
{
    // Default range = current month; a reversed range is swapped.
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var today = clock.GetLocalNow().DateTime.Date;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)).Date;
        if (end < start) (start, end) = (end, start);

        return View(new ReportsVM(start, end,
            await reportService.RevenueByEventAsync(start, end),
            await reportService.RevenueByVenueAsync(start, end),
            await reportService.GetOccupancyAsync(start, end),
            await reportService.GetAverageOccupancyByHourAsync(start, end)));
    }
}
