using System.Text;
using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class ReportsController(ReportService reportService) : AdminController
{
    public async Task<IActionResult> Index(DateTime? from, DateTime? to) =>
        View(await reportService.GetReportAsync(from, to));

    // UTF-8 with BOM so Excel shows Cyrillic.
    public async Task<IActionResult> Export(DateTime? from, DateTime? to)
    {
        var report = await reportService.GetReportAsync(from, to);
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(CsvExport.Build(report))).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"spravka-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
    }
}
