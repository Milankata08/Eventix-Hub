using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Controllers;

public class AccountController(UserService userService, SignInManager<User> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterVM());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        try
        {
            var user = await userService.RegisterAsync(vm.FirstName, vm.LastName, vm.Email, vm.Password);
            await signInManager.SignInAsync(user, isPersistent: false);
        }
        catch (BookingException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
        TempData["Success"] = "Регистрацията е успешна.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginVM { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid)
            return View(vm);
        var result = await signInManager.PasswordSignInAsync(vm.Email.Trim(), vm.Password, vm.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl!) : RedirectToAction("Index", "Home");
        ModelState.AddModelError("", result.IsLockedOut
            ? "Профилът е временно заключен. Опитайте по-късно."
            : "Грешен имейл или парола.");
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
