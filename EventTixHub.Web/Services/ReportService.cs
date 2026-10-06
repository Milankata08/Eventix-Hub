using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class ReportService(IRepository<Ticket> tickets, IRepository<Showtime> showtimes, TimeProvider clock)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    // Revenue counts only Paid tickets (BR-11).
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var today = Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var month = await tickets.Query()
            .Where(t => t.Status == TicketStatus.Paid
                && t.Order.PaidAt >= monthStart && t.Order.PaidAt < monthStart.AddMonths(1))
            .Select(t => new { t.Price, PaidAt = t.Order.PaidAt!.Value }).ToListAsync();
        return new DashboardDto(
            month.Where(t => t.PaidAt >= today && t.PaidAt < today.AddDays(1)).Sum(t => t.Price),
            month.Sum(t => t.Price), month.Count);
    }

    // Default range = current month; a reversed range is swapped. Two queries: paid tickets, showtimes.
    public async Task<SalesReport> GetReportAsync(DateTime? from, DateTime? to)
    {
        var now = Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var start = (from ?? monthStart).Date;
        var end = (to ?? monthStart.AddMonths(1).AddDays(-1)).Date;
        if (end < start) (start, end) = (end, start);
        var until = end.AddDays(1);

        var sales = await tickets.Query()
            .Where(t => t.Status == TicketStatus.Paid && t.Order.PaidAt >= start && t.Order.PaidAt < until)
            .Select(t => new Sale(t.Showtime.EventId, t.Showtime.Event.Title,
                t.Showtime.VenueId, t.Showtime.Venue.Name, t.Price))
            .ToListAsync();
        // Showtimes starting in the range; taken = sold + live holds.
        var occupancy = await showtimes.Query().Where(s => s.StartsAt >= start && s.StartsAt < until)
            .OrderBy(s => s.StartsAt).ToOccupancyRows(now).ToListAsync();
        var byHour = occupancy.GroupBy(r => r.StartsAt.Hour)
            .Select(g => new HourOccupancyDto(g.Key, g.Count(), g.Average(r => r.Percent)))
            .OrderBy(h => h.Hour).ToList();

        return new SalesReport(start, end, Revenue(sales, s => (s.EventId, s.EventTitle)),
            Revenue(sales, s => (s.VenueId, s.VenueName)), occupancy, byHour);
    }

    // Money is summed in C#, never in SQL (PLAN pitfall 6).
    private static List<RevenueRowDto> Revenue(IEnumerable<Sale> sales, Func<Sale, (int Id, string Name)> key) =>
        sales.GroupBy(key)
            .Select(g => new RevenueRowDto(g.Key.Id, g.Key.Name, g.Count(), g.Sum(s => s.Price)))
            .OrderByDescending(r => r.Revenue).ThenBy(r => r.Name).ToList();

    private record Sale(int EventId, string EventTitle, int VenueId, string VenueName, decimal Price);
}
