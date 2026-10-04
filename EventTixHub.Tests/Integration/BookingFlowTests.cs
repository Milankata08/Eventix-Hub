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
        db.AddRange(u1, u2, showtime, new PromoCode
        {
            Code = "WELCOME10", DiscountPercent = 10, UsageLimit = 100, IsActive = true,
            ValidFrom = _clock.Now.DateTime.AddDays(-1), ValidUntil = _clock.Now.DateTime.AddDays(30)
        });
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

    private OrderService Orders()
    {
        var db = _db.NewContext();
        return new OrderService(new Repository<Order>(db), new Repository<Ticket>(db),
            new Repository<PromoCode>(db), _clock, NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task FullFlow_ReserveConcessionPromoCheckout_ProducesPaidTickets()
    {
        using (var db = _db.NewContext())
        {
            var seatIds = db.Seats.OrderBy(s => s.Number).Select(s => s.Id).ToList();
            await Service().ReserveSeatsAsync(_user1, _showtimeId, seatIds);
        }
        int studentTicketId;
        using (var db = _db.NewContext())
            studentTicketId = db.Tickets.Include(t => t.Seat).Single(t => t.Seat.Number == 1).Id;

        await Orders().SetConcessionAsync(_user1, studentTicketId, ConcessionType.Student);
        await Orders().ApplyPromoAsync(_user1, "welcome10");
        var order = await Orders().CheckoutAsync(_user1);

        using var check = _db.NewContext();
        var paid = check.Orders.Include(o => o.Tickets).Single();
        Assert.Equal((OrderStatus.Paid, 25.20m), (paid.Status, paid.TotalAmount));
        Assert.All(paid.Tickets, t => Assert.Equal(TicketStatus.Paid, t.Status));
        Assert.Equal(2, paid.Tickets.Select(t => t.Code).Distinct().Count());
        Assert.Equal(1, check.PromoCodes.Single().TimesUsed);
        Assert.Equal(order.Id, paid.Id);
        Assert.Equal(2, (await Service().GetSeatMapAsync(_showtimeId))!.SoldCount);
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

    [Fact]
    public async Task CancelledTicketSeatCanBeBoughtAgain()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);
        await Orders().CheckoutAsync(_user1);
        int ticketId;
        using (var db = _db.NewContext())
            ticketId = db.Tickets.Single().Id;

        await Service().CancelTicketAsync(_user1, ticketId);
        await Service().ReserveSeatsAsync(_user2, _showtimeId, [_seatId]);

        var history = await Service().GetUserTicketsAsync(_user1, TicketHistoryFilter.Cancelled, 1);
        Assert.Single(history.Items);
        using var check = _db.NewContext();
        Assert.Equal(OrderStatus.Cancelled, check.Orders.Single(o => o.UserId == _user1).Status);
    }

    [Fact]
    public async Task HistorySplitsUpcomingAndPast_AndDetailsAreOwnerOnly()
    {
        await Service().ReserveSeatsAsync(_user1, _showtimeId, [_seatId]);
        await Orders().CheckoutAsync(_user1);

        var upcoming = await Service().GetUserTicketsAsync(_user1, TicketHistoryFilter.Upcoming, 1);
        var ticketId = Assert.Single(upcoming.Items).TicketId;
        Assert.True(upcoming.Items[0].CanCancel);
        Assert.NotNull(await Service().GetUserTicketAsync(_user1, ticketId));
        Assert.Null(await Service().GetUserTicketAsync(_user2, ticketId));

        _clock.Now = _clock.Now.AddDays(3);
        Assert.Empty((await Service().GetUserTicketsAsync(_user1, TicketHistoryFilter.Upcoming, 1)).Items);
        Assert.Single((await Service().GetUserTicketsAsync(_user1, TicketHistoryFilter.Past, 1)).Items);
    }
}
