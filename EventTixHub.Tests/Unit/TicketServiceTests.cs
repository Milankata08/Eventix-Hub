using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;

namespace EventTixHub.Tests.Unit;

public class TicketServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Showtime _showtime = new()
    {
        Id = 1, VenueId = 1, StartsAt = Now.DateTime.AddDays(2), StandardPrice = 10m, VipPrice = 20m
    };
    private readonly List<Seat> _seats = Enumerable.Range(1, 12)
        .Select(i => new Seat { Id = i, VenueId = 1, Row = 1, Number = i }).ToList();
    private readonly List<Ticket> _tickets = [];

    private TicketService Service()
    {
        var showtimes = new Mock<IRepository<Showtime>>();
        showtimes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_showtime);
        var seats = new Mock<IRepository<Seat>>();
        seats.Setup(r => r.Query()).Returns(_seats.BuildMock());
        var tickets = new Mock<IRepository<Ticket>>();
        tickets.Setup(r => r.Query()).Returns(_tickets.BuildMock());
        var orders = new Mock<IRepository<Order>>();
        orders.Setup(r => r.Query()).Returns(new List<Order>().BuildMock());
        return new TicketService(showtimes.Object, seats.Object, tickets.Object, orders.Object,
            new FakeClock(Now), NullLogger<TicketService>.Instance);
    }

    [Fact]
    public async Task Reserve_RejectsSeatThatIsAlreadySold()
    {
        _tickets.Add(new Ticket { ShowtimeId = 1, SeatId = 7, Status = TicketStatus.Paid });

        var ex = await Assert.ThrowsAsync<BookingException>(() => Service().ReserveSeatsAsync(1, 1, [7]));

        Assert.Equal("Ред 1, място 7 е заето.", ex.Message);
    }

    [Fact]
    public async Task Reserve_RejectsShowtimeThatAlreadyStarted()
    {
        _showtime.StartsAt = Now.DateTime.AddMinutes(-1);

        await Assert.ThrowsAsync<BookingException>(() => Service().ReserveSeatsAsync(1, 1, [1]));
    }

    [Fact]
    public async Task Reserve_RejectsMoreThanTenSeats()
    {
        await Assert.ThrowsAsync<BookingException>(() =>
            Service().ReserveSeatsAsync(1, 1, Enumerable.Range(1, 11).ToList()));
    }

    [Fact]
    public async Task Reserve_RejectsSeatFromAnotherVenue()
    {
        _seats[0].VenueId = 2;

        await Assert.ThrowsAsync<BookingException>(() => Service().ReserveSeatsAsync(1, 1, [1]));
    }
}
