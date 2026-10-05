using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Integration;

public class ReportServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

    public ReportServiceTests()
    {
        using var db = _db.NewContext();
        var user = new User { UserName = "u1", FirstName = "Иван", LastName = "Петров" };
        var venue = new Venue { Name = "Зала 1", City = "София", Address = "ул. Тест 1", Rows = 2, SeatsPerRow = 2 };
        foreach (var (row, number) in new[] { (1, 1), (1, 2), (2, 1), (2, 2) })
            venue.Seats.Add(new Seat { Row = row, Number = number });
        var ev = new Event { Title = "Филм", Genre = "Драма", DurationMinutes = 100, Type = EventType.Movie };
        var showtime = new Showtime
        {
            Event = ev, Venue = venue, StartsAt = new DateTime(2026, 10, 12, 19, 0, 0), StandardPrice = 10m, VipPrice = 20m
        };
        db.AddRange(user, showtime);
        db.SaveChanges();
        var order = new Order
        {
            UserId = user.Id, CreatedAt = _clock.Now.DateTime, PaidAt = _clock.Now.DateTime, Status = OrderStatus.Paid
        };
        var seats = venue.Seats.OrderBy(s => s.Number).ThenBy(s => s.Row).ToList();
        order.Tickets.Add(new Ticket { ShowtimeId = showtime.Id, SeatId = seats[0].Id, Status = TicketStatus.Paid, Price = 10m });
        order.Tickets.Add(new Ticket { ShowtimeId = showtime.Id, SeatId = seats[1].Id, Status = TicketStatus.Cancelled, Price = 10m });
        db.Add(order);
        db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private ReportService Reports()
    {
        var db = _db.NewContext();
        return new ReportService(new Repository<Ticket>(db), new Repository<Showtime>(db), _clock);
    }

    [Fact]
    public async Task GetReport_DefaultsToCurrentMonth_AndSwapsReversedRange()
    {
        var month = await Reports().GetReportAsync(null, null);
        var swapped = await Reports().GetReportAsync(new DateTime(2026, 10, 20), new DateTime(2026, 10, 5));

        Assert.Equal((new DateTime(2026, 10, 1), new DateTime(2026, 10, 31)), (month.From, month.To));
        Assert.Equal((new DateTime(2026, 10, 5), new DateTime(2026, 10, 20)), (swapped.From, swapped.To));
    }

    [Fact]
    public async Task GetReport_GroupsPaidRevenue_AndDerivesHourlyOccupancy()
    {
        var report = await Reports().GetReportAsync(null, null);

        var byEvent = Assert.Single(report.ByEvent);
        var byVenue = Assert.Single(report.ByVenue);
        Assert.Equal((1, 10.00m), (byEvent.TicketsSold, byEvent.Revenue));
        Assert.Equal((1, 10.00m), (byVenue.TicketsSold, byVenue.Revenue));
        var row = Assert.Single(report.Occupancy);
        Assert.Equal((1, 25.0), (row.Taken, row.Percent));
        var hour = Assert.Single(report.ByHour);
        Assert.Equal((19, 1, 25.0), (hour.Hour, hour.Showtimes, hour.AveragePercent));
    }
}
