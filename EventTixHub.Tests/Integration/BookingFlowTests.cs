using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTixHub.Tests.Integration;

public class BookingFlowTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));
    private int _showtimeId;
    private int _seatId;
    private int _user1;
    private int _user2;

    public BookingFlowTests()
    {
        using var db = _db.NewContext();
        var u1 = new User { UserName = "u1", FirstName = "Иван", LastName = "Петров" };
        var u2 = new User { UserName = "u2", FirstName = "Мария", LastName = "Иванова" };
        var venue = new Venue { Name = "Зала 1", City = "София", Address = "ул. Тест 1", Rows = 1, SeatsPerRow = 2 };
        venue.Seats.Add(new Seat { Row = 1, Number = 1 });
        venue.Seats.Add(new Seat { Row = 1, Number = 2, Category = SeatCategory.Vip });
        var ev = new Event { Title = "Филм", Genre = "Драма", DurationMinutes = 100, Type = EventType.Movie };
        var showtime = new Showtime
        {
            Event = ev, Venue = venue, StartsAt = _clock.Now.DateTime.AddDays(2),
            StandardPrice = 10m, VipPrice = 20m
        };
        db.AddRange(u1, u2, showtime);
        db.SaveChanges();
        _user1 = u1.Id;
        _user2 = u2.Id;
        _showtimeId = showtime.Id;
        _seatId = venue.Seats.First().Id;
    }

    public void Dispose() => _db.Dispose();

    private TicketService Service()
    {
        var db = _db.NewContext();
        return new TicketService(new Repository<Showtime>(db), new Repository<Seat>(db),
            new Repository<Ticket>(db), new Repository<Order>(db), _clock, NullLogger<TicketService>.Instance);
    }

    [Fact]
    public async Task SecondUserCannotReserveSeatHeldByFirst()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);

        await Assert.ThrowsAsync<BookingException>(() => Service().ReserveSeatsAsync(_user2, _showtimeId, [_seatId]));
    }

    [Fact]
    public async Task DatabaseRefusesSecondNonCancelledTicketForSameSeat()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);

        using var db = _db.NewContext();
        var order = new Order { UserId = _user2, CreatedAt = _clock.Now.DateTime, Status = OrderStatus.Pending };
        db.Tickets.Add(new Ticket
        {
            Order = order, ShowtimeId = _showtimeId, SeatId = _seatId, Status = TicketStatus.Reserved,
            ReservedUntil = _clock.Now.DateTime.AddMinutes(10)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task AfterHoldExpiresAnotherUserCanReserveSeat()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);
        _clock.Now = _clock.Now.AddMinutes(11);

        await Service().ReserveSeatsAsync(_user2, _showtimeId, [_seatId]);

        using var db = _db.NewContext();
        var ticket = Assert.Single(db.Tickets.Include(t => t.Order).Where(t => t.SeatId == _seatId));
        Assert.Equal(_user2, ticket.Order.UserId);
    }

    [Fact]
    public async Task SeatMapCountsFreeHeldAndSold()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);

        var map = await Service().GetSeatMapAsync(_showtimeId);

        Assert.Equal((1, 1, 0), (map!.FreeCount, map.HeldCount, map.SoldCount));
    }

    [Fact]
    public async Task RemoveFromCartFreesTheSeat()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);
        int ticketId;
        using (var db = _db.NewContext())
            ticketId = db.Tickets.Single().Id;

        await Service().RemoveFromCartAsync(_user1, ticketId);

        await Service().ReserveSeatsAsync(_user2, _showtimeId, [_seatId]);
    }
}
