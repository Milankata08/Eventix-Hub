using EventTixHub.Web.Controllers;
using EventTixHub.Web.Services;
using EventTixHub.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EventTixHub.Web.Areas.Admin.Controllers;

public class UsersController(UserService userService) : AdminController
{
    public async Task<IActionResult> Index(string? search, string? role, int page = 1) =>
        View(new AdminUsersVM(await userService.SearchAsync(search, role, page), search, role, User.GetUserId()));

    [HttpPost]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        try
        {
            await userService.SetRoleAsync(User.GetUserId(), id, role);
            TempData["Success"] = "Ролята е променена.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound()
            : View("Delete", new DeleteVM(id, "Изтриване на потребител", $"{user.FirstName} {user.LastName} ({user.Email})"));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await userService.DeleteAsync(User.GetUserId(), id);
            TempData["Success"] = "Потребителят е изтрит.";
        }
        catch (BookingException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
