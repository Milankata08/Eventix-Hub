using System.Security.Claims;
using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewComponents;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EventTixHub.Tests.Unit;

public class HoldBadgeViewComponentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CartThePageAlreadyLoaded_IsUsedWithoutAnotherQuery()
    {
        var orders = new Mock<IRepository<Order>>();
        var service = new OrderService(orders.Object, Mock.Of<IRepository<Ticket>>(), Mock.Of<IRepository<PromoCode>>(),
            Mock.Of<IRepository<Showtime>>(), Mock.Of<IRepository<Seat>>(), new FakeClock(Now), NullLogger<OrderService>.Instance);
        var line = new CartLineDto(1, 1, EventType.Movie, "Дюн", "Зала 1", Now.DateTime.AddDays(1), 1, 1,
            SeatCategory.Standard, ConcessionType.Regular, 10m, 10m, Now.DateTime.AddMinutes(5));
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            [HoldBadgeViewComponent.CartKey] = new CartDto(1, [line], null, 0, 10m, 0m, 10m, line.ReservedUntil, [])
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")], "test"));
        var badge = new HoldBadgeViewComponent(service, new FakeClock(Now))
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext { User = user }, ViewData = viewData }
            }
        };

        var result = Assert.IsType<ViewViewComponentResult>(await badge.InvokeAsync());

        Assert.Equal(300, Assert.IsType<HoldBadgeVM>(result.ViewData!.Model).SecondsLeft);
        orders.Verify(r => r.Query(), Times.Never);
    }
}
