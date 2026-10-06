using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Data;

public static class DbSeeder
{
    private record EventSeed(string Title, EventType Type, string Genre, int Minutes, string Description);

    private static readonly EventSeed[] Events =
    [
        new("Дюн: Част втора", EventType.Movie, "Фантастика", 166, "Втората част от епичната сага по романа на Франк Хърбърт."),
        new("Тайната на старата къща", EventType.Movie, "Трилър", 112, "Мистериозен трилър с неочакван край."),
        new("Лято в Созопол", EventType.Movie, "Комедия", 98, "Лека българска комедия за любов и море."),
        new("Концерт на Васил Найденов", EventType.Concert, "Поп", 120, "Най-хубавите хитове на живо."),
        new("Симфоничен оркестър: Бетовен", EventType.Concert, "Класическа музика", 105, "Деветата симфония в изпълнение на оркестъра."),
        new("Рок нощ: Остава", EventType.Concert, "Рок", 130, "Рок вечер с най-известните български банди."),
        new("Под игото", EventType.Theatre, "Драма", 150, "Постановка по романа на Иван Вазов."),
        new("Службогонци", EventType.Theatre, "Комедия", 135, "Класическа българска комедия."),
        new("Ромео и Жулиета", EventType.Theatre, "Трагедия", 140, "Безсмъртната история на Шекспир."),
    ];

    private static readonly (string Name, string City, string Address, int Rows, int Seats, int VipRows)[] Venues =
    [
        ("Зала 1 - Арена Сердика", "София", "бул. Цариградско шосе 10", 10, 14, 2),
        ("Зала 2 - Камерна", "София", "ул. Раковски 55", 6, 8, 1),
        ("Голяма зала Пловдив", "Пловдив", "ул. Княз Александър 1 №12", 8, 12, 2),
        ("Морска зала Варна", "Варна", "бул. Приморски 7", 7, 10, 1),
    ];

    // Roles are required in every environment (registration assigns Customer); demo data is not.
    public static async Task SeedRolesAsync(RoleManager<Role> roles)
    {
        foreach (var role in new[] { Roles.Administrator, Roles.Customer })
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new Role(role));
    }

    public static async Task SeedAsync(ApplicationDbContext db, UserManager<User> users, RoleManager<Role> roles, TimeProvider clock)
    {
        await SeedRolesAsync(roles);
        if (await db.Events.AnyAsync() || await db.Venues.AnyAsync() || await db.PromoCodes.AnyAsync()) return;

        var now = clock.GetLocalNow().DateTime;

        await EnsureUserAsync(users, "admin@eventtix.bg", "Admin123!", "Админ", "Администратор", Roles.Administrator);
        var demo = await EnsureUserAsync(users, "user@eventtix.bg", "User123!", "Иван", "Петров", Roles.Customer);
        var other = await EnsureUserAsync(users, "maria@eventtix.bg", "User123!", "Мария", "Георгиева", Roles.Customer);

        var venues = Venues.Select(v =>
        {
            var venue = new Venue { Name = v.Name, City = v.City, Address = v.Address, Rows = v.Rows, SeatsPerRow = v.Seats };
            for (var r = 1; r <= v.Rows; r++)
                for (var n = 1; n <= v.Seats; n++)
                    venue.Seats.Add(new Seat { Row = r, Number = n, Category = r <= v.VipRows ? SeatCategory.Vip : SeatCategory.Standard });
            return venue;
        }).ToList();

        var events = Events.Select(e => new Event
        {
            Title = e.Title, Type = e.Type, Genre = e.Genre, DurationMinutes = e.Minutes, Description = e.Description
        }).ToList();

        Showtime NewShowtime(Event ev, Venue venue, DateTime startsAt)
        {
            var (std, vip) = ev.Type == EventType.Movie ? (9m, 14m) : (35m, 60m);
            var st = new Showtime { Event = ev, Venue = venue, StartsAt = startsAt, StandardPrice = std, VipPrice = vip };
            db.Showtimes.Add(st);
            return st;
        }

        // Event i -> venue i % 4; events sharing a venue use 4-hour slots (12/16/20), so nothing overlaps.
        var upcoming = new List<Showtime>();
        for (var i = 0; i < events.Count; i++)
            for (var j = 0; j < 3; j++)
                upcoming.Add(NewShowtime(events[i], venues[i % 4],
                    now.Date.AddDays(1 + j * 4 + i % 4).AddHours(12 + i / 4 * 4)));

        var past0 = NewShowtime(events[0], venues[0], now.Date.AddDays(-2).AddHours(18));
        var past1 = NewShowtime(events[1], venues[1], now.Date.AddDays(-1).AddHours(18));

        var welcome = new PromoCode { Code = "WELCOME10", DiscountPercent = 10, UsageLimit = 100, TimesUsed = 1, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(90), IsActive = true };
        var summer = new PromoCode { Code = "SUMMER25", DiscountPercent = 25, UsageLimit = 50, ValidFrom = now.AddDays(-120), ValidUntil = now.AddDays(-30), IsActive = true };
        var limited = new PromoCode { Code = "LIMITED50", DiscountPercent = 50, UsageLimit = 1, TimesUsed = 1, ValidFrom = now.AddDays(-30), ValidUntil = now.AddDays(90), IsActive = true };
        db.PromoCodes.AddRange(welcome, summer, limited);

        var codeCounter = 0;
        void AddOrder(User user, Showtime st, PromoCode? promo, params (int Row, int Number, ConcessionType Concession)[] seats)
        {
            var created = now.AddDays(-3);
            var order = new Order { User = user, CreatedAt = created, PaidAt = created, Status = OrderStatus.Paid, PromoCode = promo };
            foreach (var (row, number, concession) in seats)
            {
                var seat = st.Venue.Seats.Single(s => s.Row == row && s.Number == number);
                order.Tickets.Add(new Ticket
                {
                    Showtime = st, Seat = seat, Status = TicketStatus.Paid, Concession = concession,
                    Price = BookingRules.TicketPrice(st.StandardPrice, st.VipPrice, seat.Category, concession, promo?.DiscountPercent ?? 0),
                    Code = $"ETH-{++codeCounter:X10}"
                });
            }
            order.TotalAmount = BookingRules.OrderTotal(order.Tickets.Select(t => t.Price));
            db.Orders.Add(order);
        }

        // Past showtimes: demo customer has Paid tickets, which enables reviews.
        AddOrder(demo, past0, null, (5, 5, ConcessionType.Regular), (5, 6, ConcessionType.Student));
        AddOrder(other, past0, null, (6, 1, ConcessionType.Regular));
        AddOrder(demo, past1, null, (3, 3, ConcessionType.Regular));

        // Upcoming showtimes: occupancy and report data. Seat (row, number) must exist in that venue.
        AddOrder(demo, upcoming[0], welcome, (1, 1, ConcessionType.Regular), (1, 2, ConcessionType.Regular), (4, 4, ConcessionType.Pensioner));
        AddOrder(other, upcoming[0], limited, (7, 7, ConcessionType.Regular));
        AddOrder(other, upcoming[3], null, (1, 3, ConcessionType.Regular), (2, 3, ConcessionType.Student));
        AddOrder(demo, upcoming[4], null, (2, 2, ConcessionType.Regular));
        AddOrder(other, upcoming[7], null, (3, 1, ConcessionType.Regular), (3, 2, ConcessionType.Regular));
        AddOrder(demo, upcoming[10], null, (4, 4, ConcessionType.Student));

        db.Reviews.AddRange(
            new Review { User = demo, Event = events[0], Rating = 5, Comment = "Страхотен филм, препоръчвам!", CreatedAt = past0.StartsAt.AddHours(4) },
            new Review { User = other, Event = events[0], Rating = 4, Comment = "Много добра визия и музика.", CreatedAt = past0.StartsAt.AddHours(5) },
            new Review { User = demo, Event = events[1], Rating = 3, Comment = "Добър, но предвидим край.", CreatedAt = past1.StartsAt.AddHours(4) });

        await db.SaveChangesAsync();
    }

    private static async Task<User> EnsureUserAsync(UserManager<User> users, string email, string password,
        string first, string last, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user != null) return user;
        user = new User { UserName = email, Email = email, EmailConfirmed = true, FirstName = first, LastName = last };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Seeding user {email} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        await users.AddToRoleAsync(user, role);
        return user;
    }
}
