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
        // ponytail: one cart query per page for signed-in users; memoize per request if it ever shows up in profiling.
        var cart = await orderService.GetCartAsync(UserClaimsPrincipal.GetUserId());
        if (cart?.EarliestExpiry is not { } expiry || cart.Lines.Count == 0)
            return Content("");
        var seconds = Math.Max(0, (int)(expiry - clock.GetLocalNow().DateTime).TotalSeconds);
        return View(new HoldBadgeVM(seconds, false));
    }
}
