using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventTixHub.Tests.Integration;

public class DbSeederTests : IDisposable
{
    private readonly TestDb _testDb = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 5, 10, 0, 0, TimeSpan.Zero));

    public void Dispose() => _testDb.Dispose();

    private async Task SeedAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => _testDb.NewContext());
        services.AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<ApplicationDbContext>();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;
        await DbSeeder.SeedAsync(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<UserManager<User>>(),
            sp.GetRequiredService<RoleManager<Role>>(), _clock);
    }

    [Fact]
    public async Task Seed_CreatesDemoData_AndSecondRunDoesNotDuplicate()
    {
        await SeedAsync();
        await SeedAsync();

        using var db = _testDb.NewContext();
        Assert.Equal(9, await db.Events.CountAsync());
        Assert.Equal(4, await db.Venues.CountAsync());
        Assert.Equal(3, await db.PromoCodes.CountAsync());
        Assert.Equal(3, await db.Users.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.All(await db.Events.Select(e => e.Showtimes.Count).ToListAsync(), c => Assert.InRange(c, 2, 4));

        var now = _clock.GetLocalNow().DateTime;
        var demoId = (await db.Users.SingleAsync(u => u.Email == "user@eventtix.bg")).Id;
        Assert.True(await db.Tickets.AnyAsync(t => t.Order.UserId == demoId && t.Status == TicketStatus.Paid && t.Showtime.StartsAt < now));
        Assert.True(await db.Tickets.AnyAsync(t => t.Status == TicketStatus.Paid && t.Showtime.StartsAt > now));
    }

    [Fact]
    public async Task Seed_WithExistingPromoCodeAndNoEvents_DoesNotSeedOrThrow()
    {
        using (var db = _testDb.NewContext())
        {
            db.PromoCodes.Add(new PromoCode { Code = "WELCOME10", DiscountPercent = 5, UsageLimit = 1, ValidFrom = DateTime.Today, ValidUntil = DateTime.Today.AddDays(1), IsActive = true });
            await db.SaveChangesAsync();
        }

        await SeedAsync();

        using var check = _testDb.NewContext();
        Assert.Equal(1, await check.PromoCodes.CountAsync());
        Assert.Equal(0, await check.Events.CountAsync());
    }

    [Fact]
    public async Task Seed_ShowtimesDoNotOverlapInSameVenue()
    {
        await SeedAsync();

        using var db = _testDb.NewContext();
        var showtimes = await db.Showtimes.Include(s => s.Event).ToListAsync();
        foreach (var a in showtimes)
            foreach (var b in showtimes.Where(b => b.Id > a.Id && b.VenueId == a.VenueId))
                Assert.False(Web.Services.BookingRules.Overlaps(a.StartsAt, a.Event.DurationMinutes, b.StartsAt, b.Event.DurationMinutes),
                    $"{a.Event.Title} overlaps {b.Event.Title}");
    }
}
