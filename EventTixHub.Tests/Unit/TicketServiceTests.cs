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

    private readonly Showtime _showtime = new() { StartsAt = Now.DateTime.AddDays(2) };
    private readonly List<Ticket> _tickets = [];
    private readonly Mock<IRepository<Ticket>> _ticketRepo = new();

    private TicketService Service()
    {
        _ticketRepo.Setup(r => r.Query()).Returns(_tickets.BuildMock());
        return new TicketService(new Mock<IRepository<Showtime>>().Object, new Mock<IRepository<Seat>>().Object,
            _ticketRepo.Object, new FakeClock(Now), NullLogger<TicketService>.Instance);
    }

    private Ticket PaidTicket(int ownerId = 1) => new()
    {
        Id = 5, OrderId = 9, SeatId = 3, Status = TicketStatus.Paid, Showtime = _showtime,
        Order = new Order { Id = 9, UserId = ownerId, Status = OrderStatus.Paid }
    };

    [Fact]
    public async Task Cancel_RejectsSomeoneElsesTicket()
    {
        _tickets.Add(PaidTicket(ownerId: 2));

        await Assert.ThrowsAsync<BookingException>(() => Service().CancelTicketAsync(1, 5));
        _ticketRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cancel_BeforeDeadline_MarksTicketCancelledAndSaves()
    {
        var ticket = PaidTicket();
        _tickets.Add(ticket);

        await Service().CancelTicketAsync(1, 5);

        Assert.Equal((TicketStatus.Cancelled, Now.DateTime), (ticket.Status, ticket.CancelledAt));
        Assert.Equal(OrderStatus.Cancelled, ticket.Order.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Cancel_AfterDeadline_IsRejectedAndNothingSaved()
    {
        _showtime.StartsAt = Now.DateTime.AddHours(24).AddMinutes(-1);
        var ticket = PaidTicket();
        _tickets.Add(ticket);

        await Assert.ThrowsAsync<BookingException>(() => Service().CancelTicketAsync(1, 5));

        Assert.Equal(TicketStatus.Paid, ticket.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task Cancel_ExactlyAtDeadline_IsAllowed()
    {
        _showtime.StartsAt = Now.DateTime.AddHours(24);
        var ticket = PaidTicket();
        _tickets.Add(ticket);

        await Service().CancelTicketAsync(1, 5);

        Assert.Equal(TicketStatus.Cancelled, ticket.Status);
    }

    [Fact]
    public async Task Cancel_OneOfSeveralTickets_KeepsOrderPaid()
    {
        var ticket = PaidTicket();
        var other = new Ticket { Id = 6, OrderId = 9, SeatId = 4, Status = TicketStatus.Paid, Order = ticket.Order };
        _tickets.AddRange([ticket, other]);

        await Service().CancelTicketAsync(1, 5);

        Assert.Equal((TicketStatus.Cancelled, OrderStatus.Paid), (ticket.Status, ticket.Order.Status));
    }
}
