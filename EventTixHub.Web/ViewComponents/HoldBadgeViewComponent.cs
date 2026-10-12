using EventTixHub.Web.Controllers;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.ViewComponents;

public class HoldBadgeViewComponent(OrderService orderService, TimeProvider clock) : ViewComponent
{
    // A page that already loaded the cart puts it in ViewData under this key, so the badge does not query again.
    public const string CartKey = "HoldBadge.Cart";

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Content("");
        var cart = ViewData.TryGetValue(CartKey, out var loaded)
            ? loaded as CartDto
            : await orderService.GetCartAsync(UserClaimsPrincipal.GetUserId());
        if (cart is null || cart.Lines.Count == 0)
            return Content("");
        return View(new HoldBadgeVM(cart.SecondsLeft(clock.GetLocalNow().DateTime), false));
    }
}
