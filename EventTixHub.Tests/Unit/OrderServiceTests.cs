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
    private readonly Mock<IRepository<Order>> _orders = new();

    private OrderService Service()
    {
        _orders.Setup(r => r.Query()).Returns(new List<Order> { _cart }.BuildMock());
        var promos = new Mock<IRepository<PromoCode>>();
        promos.Setup(r => r.Query()).Returns(_promos.BuildMock());
        return new OrderService(_orders.Object, new Mock<IRepository<Ticket>>().Object, promos.Object,
            new FakeClock(Now), NullLogger<OrderService>.Instance);
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
