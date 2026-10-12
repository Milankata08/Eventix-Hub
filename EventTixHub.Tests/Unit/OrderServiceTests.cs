using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;

namespace EventTixHub.Tests.Unit;

public class OrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Order _cart = new() { Id = 1, UserId = 1, Status = OrderStatus.Pending };
    private readonly List<PromoCode> _promos = [];
    private readonly Showtime _showtime = new()
    {
        Id = 1, VenueId = 1, StartsAt = Now.DateTime.AddDays(2), StandardPrice = 10m, VipPrice = 20m
    };
    private readonly List<Seat> _seats = Enumerable.Range(1, 12)
        .Select(i => new Seat { Id = i, VenueId = 1, Row = 1, Number = i }).ToList();
    private readonly List<Ticket> _tickets = [];
    private readonly Mock<IRepository<Order>> _orders = new();

    private OrderService Service()
    {
        _orders.Setup(r => r.Query()).Returns(new List<Order> { _cart }.BuildMock());
        var promos = new Mock<IRepository<PromoCode>>();
        promos.Setup(r => r.Query()).Returns(_promos.BuildMock());
        var tickets = new Mock<IRepository<Ticket>>();
        tickets.Setup(r => r.Query()).Returns(_tickets.BuildMock());
        var showtimes = new Mock<IRepository<Showtime>>();
        showtimes.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_showtime);
        var seats = new Mock<IRepository<Seat>>();
        seats.Setup(r => r.Query()).Returns(_seats.BuildMock());
        return new OrderService(_orders.Object, tickets.Object, promos.Object, showtimes.Object, seats.Object,
            new FakeClock(Now), NullLogger<OrderService>.Instance);
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

    [Fact]
    public async Task ApplyPromo_RejectsExhaustedCode()
    {
        _promos.Add(new PromoCode
        {
            Id = 1, Code = "LIMITED50", DiscountPercent = 50, UsageLimit = 1, TimesUsed = 1, IsActive = true,
            ValidFrom = Now.DateTime.AddDays(-1), ValidUntil = Now.DateTime.AddDays(1)
        });

        await Assert.ThrowsAsync<BookingException>(() => Service().ApplyPromoAsync(1, "limited50"));

        _orders.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetCart_CountsExpiredHoldsAndKeepsActiveOnes()
    {
        var showtime = new Showtime
        {
            StartsAt = Now.DateTime.AddDays(1), StandardPrice = 9m, VipPrice = 14m,
            Event = new Event { Title = "Филм" }, Venue = new Venue { Name = "Зала 1" }
        };
        var seat = new Seat { Row = 1, Number = 1, Category = SeatCategory.Standard };
        _cart.Tickets.Add(new Ticket
        {
            Id = 1, Status = TicketStatus.Reserved, ReservedUntil = Now.DateTime.AddMinutes(-1),
            Showtime = showtime, Seat = seat
        });
        _cart.Tickets.Add(new Ticket
        {
            Id = 2, Status = TicketStatus.Reserved, ReservedUntil = Now.DateTime.AddMinutes(5),
            Showtime = showtime, Seat = seat
        });

        var cart = await Service().GetCartAsync(1);

        Assert.Equal(1, cart!.ExpiredCount);
        Assert.Single(cart.Lines);
    }

    [Fact]
    public async Task GetCart_ExpiredHold_ListsTheSeat()
    {
        _cart.Tickets.Add(new Ticket
        {
            Id = 1, ShowtimeId = 1, SeatId = 5, Status = TicketStatus.Reserved, ReservedUntil = Now.DateTime.AddMinutes(-1),
            Showtime = new Showtime { StartsAt = Now.DateTime.AddDays(1), Event = new Event { Title = "Дюн" }, Venue = new Venue() },
            Seat = new Seat { Id = 5, Row = 5, Number = 7 }
        });

        var cart = await Service().GetCartAsync(1);

        var seat = Assert.Single(cart!.Expired);
        Assert.Equal((5, 7, 5), (seat.Row, seat.Number, seat.SeatId));
    }

    private Ticket LiveHold(int id) => new()
    {
        Id = id, ShowtimeId = 1, SeatId = id, Status = TicketStatus.Reserved, ReservedUntil = Now.DateTime.AddMinutes(5),
        Showtime = new Showtime { StartsAt = Now.DateTime.AddDays(1), Event = new Event { Title = "Дюн" }, Venue = new Venue() },
        Seat = new Seat { Id = id, Row = 1, Number = id }
    };

    [Fact]
    public async Task GetCart_SecondsLeft_CountsDownToTheEarliestHold()
    {
        var later = LiveHold(2);
        later.ReservedUntil = Now.DateTime.AddMinutes(9);
        _cart.Tickets.Add(LiveHold(1));
        _cart.Tickets.Add(later);

        var cart = await Service().GetCartAsync(1);

        Assert.Equal((300, 0), (cart!.SecondsLeft(Now.DateTime), cart.SecondsLeft(Now.DateTime.AddMinutes(6))));
    }

    [Fact]
    public async Task GetCart_AfterAChangeInTheSameRequest_ShowsTheChange()
    {
        _cart.Tickets.Add(LiveHold(1));
        var service = Service();

        await service.GetCartAsync(1);
        await service.SetConcessionAsync(1, 1, ConcessionType.Student);
        var cart = await service.GetCartAsync(1);

        Assert.Equal(ConcessionType.Student, Assert.Single(cart!.Lines).Concession);
    }

    [Fact]
    public async Task Checkout_RejectsCartWhoseHoldExpired()
    {
        _cart.Tickets.Add(new Ticket
        {
            Id = 1, Status = TicketStatus.Reserved, ReservedUntil = Now.DateTime.AddMinutes(-1)
        });

        var ex = await Assert.ThrowsAsync<BookingException>(() => Service().CheckoutAsync(1));

        Assert.Equal("Резервацията изтече. Изберете местата отново.", ex.Message);
        Assert.Equal(OrderStatus.Pending, _cart.Status);
    }
}
