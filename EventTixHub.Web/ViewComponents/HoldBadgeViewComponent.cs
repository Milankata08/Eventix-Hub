using EventTixHub.Web.Controllers;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.ViewComponents;

public class HoldBadgeViewComponent(OrderService orderService, TimeProvider clock) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Content("");
        var cart = await orderService.GetCartAsync(UserClaimsPrincipal.GetUserId());
        if (cart?.EarliestExpiry is not { } expiry || cart.Lines.Count == 0)
            return Content("");
        return View(new HoldBadgeVM(BookingRules.SecondsLeft(expiry, clock.GetLocalNow().DateTime), false));
    }
}
