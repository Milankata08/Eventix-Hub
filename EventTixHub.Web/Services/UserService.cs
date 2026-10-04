using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class UserService(UserManager<User> users, IRepository<Order> orders)
{
    public async Task<User> RegisterAsync(string firstName, string lastName, string email, string password)
    {
        var user = new User
        {
            FirstName = firstName.Trim(), LastName = lastName.Trim(),
            Email = email.Trim(), UserName = email.Trim()
        };
        Check(await users.CreateAsync(user, password));
        Check(await users.AddToRoleAsync(user, Roles.Customer));
        return user;
    }

    public Task<User?> GetByIdAsync(int id) => users.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task UpdateProfileAsync(int userId, string firstName, string lastName, string? phoneNumber)
    {
        var user = await GetByIdAsync(userId) ?? throw new BookingException("Потребителят не е намерен.");
        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Check(await users.UpdateAsync(user));
    }

    public async Task<PagedResult<UserRowDto>> SearchAsync(string? search, string? role, int page, int pageSize = 10)
    {
        var query = users.Users;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(u => u.FirstName.Contains(s) || u.LastName.Contains(s) || u.Email!.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(role))
        {
            var ids = (await users.GetUsersInRoleAsync(role)).Select(u => u.Id).ToList();
            query = query.Where(u => ids.Contains(u.Id));
        }

        var paged = await query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.Id)
            .ToPagedResultAsync(page, pageSize);
        var rows = new List<UserRowDto>();
        foreach (var u in paged.Items)
        {
            var userRoles = await users.GetRolesAsync(u);
            rows.Add(new UserRowDto(u.Id, $"{u.FirstName} {u.LastName}", u.Email ?? "",
                userRoles.Contains(Roles.Administrator) ? Roles.Administrator : Roles.Customer));
        }
        return new PagedResult<UserRowDto>(rows, paged.Page, paged.PageSize, paged.TotalCount);
    }

    // BR-16
    public async Task SetRoleAsync(int actingUserId, int targetId, string role)
    {
        if (role is not (Roles.Administrator or Roles.Customer))
            throw new BookingException("Невалидна роля.");
        if (actingUserId == targetId)
            throw new BookingException("Не можете да променяте собствената си роля.");
        var user = await GetByIdAsync(targetId) ?? throw new BookingException("Потребителят не е намерен.");

        var current = await users.GetRolesAsync(user);
        if (current.Count == 1 && current[0] == role)
            return;
        // Add first: a failure never leaves the user without a role.
        Check(await users.AddToRoleAsync(user, role));
        Check(await users.RemoveFromRolesAsync(user, current.Where(r => r != role).ToList()));
    }

    // BR-15, BR-16
    public async Task DeleteAsync(int actingUserId, int targetId)
    {
        if (actingUserId == targetId)
            throw new BookingException("Не можете да изтриете себе си.");
        var user = await GetByIdAsync(targetId) ?? throw new BookingException("Потребителят не е намерен.");
        if (await orders.Query().AnyAsync(o => o.UserId == targetId))
            throw new BookingException("Потребител с поръчки не може да се изтрие.");
        try
        {
            Check(await users.DeleteAsync(user));
        }
        catch (DbUpdateException)
        {
            // Restrict FK: an order appeared after the check.
            throw new BookingException("Потребител с поръчки не може да се изтрие.");
        }
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BookingException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
