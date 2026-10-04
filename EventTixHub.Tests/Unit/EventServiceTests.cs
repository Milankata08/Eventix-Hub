using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using MockQueryable;
using Moq;

namespace EventTixHub.Tests.Unit;

public class EventServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Event _event = new() { Id = 1, Title = "Филм", Genre = "Драма", DurationMinutes = 120 };
    private readonly List<Showtime> _showtimes = [];
    private readonly List<Ticket> _tickets = [];
    private readonly Mock<IRepository<Showtime>> _showtimeRepo = new();

    public EventServiceTests()
    {
        _showtimes.Add(new Showtime
        {
            Id = 1, EventId = 1, Event = _event, VenueId = 1, StartsAt = Now.DateTime.AddDays(1).Date.AddHours(18)
        });
    }

    private EventService Service()
    {
        var events = new Mock<IRepository<Event>>();
        events.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_event);
        events.Setup(r => r.Query()).Returns(new List<Event> { _event }.BuildMock());
        _showtimeRepo.Setup(r => r.Query()).Returns(_showtimes.BuildMock());
        var tickets = new Mock<IRepository<Ticket>>();
        tickets.Setup(r => r.Query()).Returns(_tickets.BuildMock());
        return new EventService(events.Object, _showtimeRepo.Object, tickets.Object,
            new Mock<IRepository<Venue>>().Object, new FakeClock(Now));
    }

    private Showtime NewShowtime(DateTime startsAt) =>
        new() { EventId = 1, VenueId = 1, StartsAt = startsAt, StandardPrice = 10m, VipPrice = 20m };

    [Fact]
    public async Task CreateShowtime_RejectsOverlapInSameVenue()
    {
        // Existing 18:00-20:00 tomorrow; new one starts 19:00.
        var overlapping = NewShowtime(_showtimes[0].StartsAt.AddHours(1));

        await Assert.ThrowsAsync<BookingException>(() => Service().CreateShowtimeAsync(overlapping));

        _showtimeRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateShowtime_AllowsBackToBack()
    {
        var next = NewShowtime(_showtimes[0].StartsAt.AddMinutes(120));

        await Service().CreateShowtimeAsync(next);

        _showtimeRepo.Verify(r => r.AddAsync(next), Times.Once);
    }

    [Fact]
    public async Task CreateShowtime_RejectsStartInThePast()
    {
        await Assert.ThrowsAsync<BookingException>(() =>
            Service().CreateShowtimeAsync(NewShowtime(Now.DateTime.AddMinutes(-1))));
    }

    [Fact]
    public async Task DeleteShowtime_WithTickets_IsRefused()
    {
        _showtimes[0].Tickets.Add(new Ticket { ShowtimeId = 1 });
        _tickets.Add(new Ticket { ShowtimeId = 1 });

        await Assert.ThrowsAsync<BookingException>(() => Service().DeleteShowtimeAsync(1));

        _showtimeRepo.Verify(r => r.Remove(It.IsAny<Showtime>()), Times.Never);
    }
}
