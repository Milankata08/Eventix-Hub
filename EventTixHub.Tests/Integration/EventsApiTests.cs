using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Controllers.Api;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Tests.Integration;

public class EventsApiTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));
    private int _eventId;

    public EventsApiTests()
    {
        using var db = _db.NewContext();
        var venue = new Venue { Name = "Зала 1", City = "София", Address = "ул. Тест 1", Rows = 1, SeatsPerRow = 1 };
        venue.Seats.Add(new Seat { Row = 1, Number = 1 });
        var ev = new Event { Title = "Филм", Genre = "Драма", DurationMinutes = 100, Type = EventType.Movie };
        db.Add(new Showtime
        {
            Event = ev, Venue = venue, StartsAt = _clock.Now.DateTime.AddDays(2), StandardPrice = 10m, VipPrice = 20m
        });
        db.SaveChanges();
        _eventId = ev.Id;
    }

    public void Dispose() => _db.Dispose();

    private EventsApiController Api()
    {
        var db = _db.NewContext();
        return new EventsApiController(new EventService(new Repository<Event>(db), new Repository<Showtime>(db),
            new Repository<Ticket>(db), new Repository<Venue>(db), _clock));
    }

    [Fact]
    public async Task List_returns_paged_events()
    {
        var page = await Api().List(new EventFilter());
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Филм", page.Items[0].Title);
    }

    [Fact]
    public async Task Get_returns_dto_with_showtimes_or_404()
    {
        var api = Api();
        var ok = await api.Get(_eventId);
        Assert.Single(ok.Value!.Showtimes);
        Assert.IsType<NotFoundResult>((await api.Get(9999)).Result);
    }
}
