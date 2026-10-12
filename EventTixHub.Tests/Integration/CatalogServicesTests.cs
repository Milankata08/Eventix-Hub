using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Integration;

public class CatalogServicesTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly TempWebRoot _web = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));
    private int _movieId, _concertId, _venueId, _showtimeId, _userId;

    public CatalogServicesTests()
    {
        using var db = _db.NewContext();
        var user = new User { UserName = "u1", FirstName = "Иван", LastName = "Петров" };
        var venue = new Venue { Name = "Зала 1", City = "София", Address = "ул. Тест 1", Rows = 2, SeatsPerRow = 2 };
        venue.Seats.Add(new Seat { Row = 1, Number = 1 });
        venue.Seats.Add(new Seat { Row = 1, Number = 2 });
        venue.Seats.Add(new Seat { Row = 2, Number = 1 });
        venue.Seats.Add(new Seat { Row = 2, Number = 2 });
        var movie = new Event { Title = "Филм", Genre = "Драма", DurationMinutes = 100, Type = EventType.Movie };
        var concert = new Event { Title = "Концерт", Genre = "Рок", DurationMinutes = 90, Type = EventType.Concert };
        var showtime = new Showtime
        {
            Event = movie, Venue = venue, StartsAt = _clock.Now.DateTime.AddDays(2), StandardPrice = 10m, VipPrice = 20m
        };
        db.AddRange(user, concert, showtime);
        db.SaveChanges();
        db.Add(new Review { UserId = user.Id, EventId = movie.Id, Rating = 4, CreatedAt = _clock.Now.DateTime });
        db.Add(new Ticket
        {
            Order = new Order { UserId = user.Id, CreatedAt = _clock.Now.DateTime, Status = OrderStatus.Paid },
            ShowtimeId = showtime.Id, SeatId = venue.Seats.First().Id, Status = TicketStatus.Paid, Price = 10m
        });
        db.SaveChanges();
        (_movieId, _concertId, _venueId, _showtimeId, _userId) = (movie.Id, concert.Id, venue.Id, showtime.Id, user.Id);
    }

    public void Dispose()
    {
        _db.Dispose();
        _web.Dispose();
    }

    private EventService Events()
    {
        var db = _db.NewContext();
        return new EventService(new Repository<Event>(db), new Repository<Showtime>(db),
            new Repository<Ticket>(db), new Repository<Venue>(db), _web.Posters, _clock);
    }

    private VenueService Venues()
    {
        var db = _db.NewContext();
        return new VenueService(new Repository<Venue>(db), new Repository<Seat>(db), new Repository<Showtime>(db));
    }

    [Fact]
    public async Task Search_FiltersByCityAndSortsEventsWithoutShowtimesLast()
    {
        var all = await Events().SearchAsync(new EventFilter(), 1);
        var sofia = await Events().SearchAsync(new EventFilter(City: "София"), 1);
        var plovdiv = await Events().SearchAsync(new EventFilter(City: "Пловдив"), 1);

        Assert.Equal(["Филм", "Концерт"], all.Items.Select(i => i.Title));
        Assert.Equal(["Филм"], sofia.Items.Select(i => i.Title));
        Assert.Empty(plovdiv.Items);
        Assert.Equal(4.0, all.Items[0].AverageRating);
        Assert.Null(all.Items[1].AverageRating);
    }

    [Fact]
    public async Task Search_FromPriceIsLowestStandardPriceOfUpcomingShowtimes()
    {
        using (var db = _db.NewContext())
        {
            // A cheaper showtime that already started must not count; a dearer upcoming one must not win.
            db.AddRange(
                new Showtime { EventId = _movieId, VenueId = _venueId, StartsAt = _clock.Now.DateTime.AddHours(-1), StandardPrice = 5m, VipPrice = 8m },
                new Showtime { EventId = _movieId, VenueId = _venueId, StartsAt = _clock.Now.DateTime.AddDays(3), StandardPrice = 12.5m, VipPrice = 18m });
            db.SaveChanges();
        }

        var items = (await Events().SearchAsync(new EventFilter(), 1)).Items;

        Assert.Equal(10m, items.Single(i => i.Id == _movieId).MinPrice);
        Assert.Null(items.Single(i => i.Id == _concertId).MinPrice);
    }

    [Fact]
    public async Task Search_FromPriceCountsOnlyShowtimesMatchingTheFilter()
    {
        using (var db = _db.NewContext())
        {
            var varna = new Venue { Name = "Морска зала", City = "Варна", Address = "ул. Тест 2", Rows = 1, SeatsPerRow = 1 };
            db.Add(new Showtime { EventId = _movieId, Venue = varna, StartsAt = _clock.Now.DateTime.AddDays(3), StandardPrice = 20m, VipPrice = 30m });
            db.SaveChanges();
        }

        var inVarna = (await Events().SearchAsync(new EventFilter(City: "Варна"), 1)).Items.Single();

        // The 10,00 € showtime is in София, so a Варна buyer must not see it.
        Assert.Equal((20m, "Варна"), (inVarna.MinPrice, inVarna.NextCity));
    }

    [Fact]
    public async Task Details_ShowsUpcomingShowtimeWithFreeSeatCount()
    {
        var details = await Events().GetDetailsAsync(_movieId);

        Assert.NotNull(details);
        Assert.Equal((1, 4.0), (details.ReviewCount, details.AverageRating));
        Assert.Equal(3, details.Showtimes.Single().FreeSeats);
    }

    [Fact]
    public async Task Occupancy_CountsSoldAndFree()
    {
        var row = Assert.Single(await Events().GetEventOccupancyAsync(_movieId));

        Assert.Equal((4, 1, 0, 3), (row.Capacity, row.Sold, row.Held, row.Free));
        Assert.Equal((1, 25.0, false), (row.Taken, row.Percent, row.IsCritical));
    }

    [Fact]
    public async Task SearchShowtimes_DefaultsToNotYetStarted_PastListsTheRestNewestFirst()
    {
        var now = _clock.Now.DateTime;
        using (var db = _db.NewContext())
        {
            db.AddRange(
                new Showtime { EventId = _concertId, VenueId = _venueId, StartsAt = now.AddHours(-1), StandardPrice = 10m, VipPrice = 20m },
                new Showtime { EventId = _concertId, VenueId = _venueId, StartsAt = now.AddDays(-3), StandardPrice = 10m, VipPrice = 20m });
            db.SaveChanges();
        }

        var upcoming = await Events().SearchShowtimesAsync(new ShowtimeFilter(), 1);
        var past = await Events().SearchShowtimesAsync(new ShowtimeFilter(Past: true), 1);

        Assert.Equal([_showtimeId], upcoming.Items.Select(i => i.Id));
        Assert.Equal([now.AddHours(-1), now.AddDays(-3)], past.Items.Select(i => i.StartsAt));
    }

    [Fact]
    public async Task DeleteEvent_WithTickets_IsRefused_ButEventWithoutIsDeleted()
    {
        await Assert.ThrowsAsync<BookingException>(() => Events().DeleteEventAsync(_movieId));

        await Events().DeleteEventAsync(_concertId);

        Assert.Null(await Events().GetEventAsync(_concertId));
    }

    private static Event NewEvent() =>
        new() { Title = "Нов", Genre = "Джаз", DurationMinutes = 60, Type = EventType.Concert };

    [Fact]
    public async Task UpdateEvent_ReplacingPoster_DeletesOldFile()
    {
        var ev = await Events().CreateEventAsync(NewEvent(), TempWebRoot.Png());
        var oldUrl = ev.PosterUrl;

        await Events().UpdateEventAsync(
            new Event { Id = ev.Id, Title = "Нов", Genre = "Джаз", DurationMinutes = 60 }, TempWebRoot.Png(), false);

        var stored = (await Events().GetEventAsync(ev.Id))!.PosterUrl;
        Assert.NotEqual(oldUrl, stored);
        Assert.Equal([Path.GetFileName(stored)], _web.Files().Select(Path.GetFileName));
    }

    [Fact]
    public async Task UpdateEvent_RemovePoster_ClearsUrlAndDeletesFile()
    {
        var ev = await Events().CreateEventAsync(NewEvent(), TempWebRoot.Png());

        await Events().UpdateEventAsync(
            new Event { Id = ev.Id, Title = "Нов", Genre = "Джаз", DurationMinutes = 60 }, null, true);

        Assert.Null((await Events().GetEventAsync(ev.Id))!.PosterUrl);
        Assert.Empty(_web.Files());
    }

    [Fact]
    public async Task DeleteEvent_DeletesPosterFile()
    {
        var ev = await Events().CreateEventAsync(NewEvent(), TempWebRoot.Png());
        Assert.Single(_web.Files());

        await Events().DeleteEventAsync(ev.Id);

        Assert.Empty(_web.Files());
    }

    [Fact]
    public async Task GetShowtime_LoadsEventAndVenue()
    {
        var showtime = await Events().GetShowtimeAsync(_showtimeId);

        Assert.NotNull(showtime);
        Assert.Equal(("Филм", "Зала 1"), (showtime.Event.Title, showtime.Venue.Name));
        Assert.Null(await Events().GetShowtimeAsync(999));
    }

    [Fact]
    public async Task CreateVenue_GeneratesStandardSeatGrid()
    {
        var venue = await Venues().CreateAsync(
            new Venue { Name = "Нова", City = "Варна", Address = "ул. Нова 2", Rows = 3, SeatsPerRow = 4 });

        var seats = await Venues().GetSeatsAsync(venue.Id);

        Assert.Equal(12, seats.Count);
        Assert.All(seats, s => Assert.Equal(SeatCategory.Standard, s.Category));
    }

    [Fact]
    public async Task ToggleRowVip_FlipsWholeRowAndBack()
    {
        await Venues().ToggleRowVipAsync(_venueId, 1);
        var afterFirst = (await Venues().GetSeatsAsync(_venueId)).Where(s => s.Row == 1).Select(s => s.Category);
        await Venues().ToggleRowVipAsync(_venueId, 1);
        var afterSecond = (await Venues().GetSeatsAsync(_venueId)).Where(s => s.Row == 1).Select(s => s.Category);

        Assert.All(afterFirst, c => Assert.Equal(SeatCategory.Vip, c));
        Assert.All(afterSecond, c => Assert.Equal(SeatCategory.Standard, c));
    }

    [Fact]
    public async Task UpdateVenue_GridChangeRefusedWithShowtimes_AllowedWithout()
    {
        await Assert.ThrowsAsync<BookingException>(() => Venues().UpdateAsync(
            new Venue { Id = _venueId, Name = "Зала 1", City = "София", Address = "ул. Тест 1", Rows = 3, SeatsPerRow = 2 }));

        var free = await Venues().CreateAsync(
            new Venue { Name = "Нова", City = "Варна", Address = "ул. Нова 2", Rows = 2, SeatsPerRow = 2 });
        await Venues().UpdateAsync(
            new Venue { Id = free.Id, Name = "Нова", City = "Варна", Address = "ул. Нова 2", Rows = 3, SeatsPerRow = 1 });

        var seats = await Venues().GetSeatsAsync(free.Id);
        Assert.Equal([(1, 1), (2, 1), (3, 1)], seats.Select(s => (s.Row, s.Number)));
    }

    [Fact]
    public async Task Reviews_PagedWithAuthorName_AndCanReviewOnlyOncePerEvent()
    {
        var db = _db.NewContext();
        var service = new ReviewService(new Repository<Review>(db), new Repository<Ticket>(db), _clock);

        var page = await service.GetForEventAsync(_movieId, 1);

        Assert.Equal("Иван Петров", Assert.Single(page.Items).Author);
        Assert.False(await service.CanReviewAsync(_userId, _movieId));
    }
}
