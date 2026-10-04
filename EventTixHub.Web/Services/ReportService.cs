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

    public async Task<IReadOnlyList<RevenueRowDto>> RevenueByEventAsync(DateTime? from, DateTime? to) =>
        (await PaidInRange(from, to)
            .Select(t => new { Id = t.Showtime.EventId, Name = t.Showtime.Event.Title, t.Price }).ToListAsync())
        .GroupBy(t => (t.Id, t.Name))
        .Select(g => new RevenueRowDto(g.Key.Id, g.Key.Name, g.Count(), g.Sum(t => t.Price)))
        .OrderByDescending(r => r.Revenue).ThenBy(r => r.Name).ToList();

    public async Task<IReadOnlyList<RevenueRowDto>> RevenueByVenueAsync(DateTime? from, DateTime? to) =>
        (await PaidInRange(from, to)
            .Select(t => new { Id = t.Showtime.VenueId, Name = t.Showtime.Venue.Name, t.Price }).ToListAsync())
        .GroupBy(t => (t.Id, t.Name))
        .Select(g => new RevenueRowDto(g.Key.Id, g.Key.Name, g.Count(), g.Sum(t => t.Price)))
        .OrderByDescending(r => r.Revenue).ThenBy(r => r.Name).ToList();

    // Showtimes starting in the range; taken = sold + live holds.
    public async Task<IReadOnlyList<OccupancyRowDto>> GetOccupancyAsync(DateTime? from, DateTime? to)
    {
        var now = Now;
        var query = showtimes.Query();
        if (from is { } f) query = query.Where(s => s.StartsAt >= f.Date);
        if (to is { } t) query = query.Where(s => s.StartsAt < t.Date.AddDays(1));
        var rows = await query.OrderBy(s => s.StartsAt)
            .Select(s => new
            {
                s.Id, s.Event.Title, VenueName = s.Venue.Name, s.StartsAt,
                Capacity = s.Venue.Rows * s.Venue.SeatsPerRow,
                Taken = s.Tickets.Count(x => x.Status == TicketStatus.Paid
                    || (x.Status == TicketStatus.Reserved && x.ReservedUntil > now))
            }).ToListAsync();
        return rows.Select(r =>
        {
            var percent = r.Capacity == 0 ? 0 : 100.0 * r.Taken / r.Capacity;
            return new OccupancyRowDto(r.Id, r.Title, r.VenueName, r.StartsAt, r.Capacity, r.Taken, percent,
                percent >= BookingRules.CriticalOccupancyPercent);
        }).ToList();
    }

    public async Task<IReadOnlyList<HourOccupancyDto>> GetAverageOccupancyByHourAsync(DateTime? from, DateTime? to) =>
        (await GetOccupancyAsync(from, to))
        .GroupBy(r => r.StartsAt.Hour)
        .Select(g => new HourOccupancyDto(g.Key, g.Count(), g.Average(r => r.Percent)))
        .OrderBy(h => h.Hour).ToList();

    private IQueryable<Ticket> PaidInRange(DateTime? from, DateTime? to)
    {
        var query = tickets.Query().Where(t => t.Status == TicketStatus.Paid);
        if (from is { } f) query = query.Where(t => t.Order.PaidAt >= f.Date);
        if (to is { } e) query = query.Where(t => t.Order.PaidAt < e.Date.AddDays(1));
        return query;
    }
}
