using System.Text;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class ReportsController(ReportService reportService, TimeProvider clock) : AdminController
{
    public async Task<IActionResult> Index(DateTime? from, DateTime? to) => View(await BuildAsync(from, to));

    // UTF-8 with BOM so Excel shows Cyrillic.
    public async Task<IActionResult> Export(DateTime? from, DateTime? to)
    {
        var vm = await BuildAsync(from, to);
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(CsvExport.Build(vm))).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"spravka-{vm.From:yyyyMMdd}-{vm.To:yyyyMMdd}.csv");
    }

    // Default range = current month; a reversed range is swapped.
    private async Task<ReportsVM> BuildAsync(DateTime? from, DateTime? to)
    {
        var today = clock.GetLocalNow().DateTime.Date;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)).Date;
        if (end < start) (start, end) = (end, start);

        return new ReportsVM(start, end,
            await reportService.RevenueByEventAsync(start, end),
            await reportService.RevenueByVenueAsync(start, end),
            await reportService.GetOccupancyAsync(start, end),
            await reportService.GetAverageOccupancyByHourAsync(start, end));
    }
}
