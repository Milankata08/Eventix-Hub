using EventTixHub.Tests.TestHelpers;
using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;

namespace EventTixHub.Tests.Integration;

public class PromoCodeServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

    public PromoCodeServiceTests()
    {
        using var db = _db.NewContext();
        var now = _clock.Now.DateTime;
        PromoCode Promo(string code, bool active, int fromDays, int untilDays, int used) => new()
        {
            Code = code, DiscountPercent = 10, UsageLimit = 5, TimesUsed = used, IsActive = active,
            ValidFrom = now.AddDays(fromDays), ValidUntil = now.AddDays(untilDays)
        };
        db.AddRange(
            Promo("USABLE", true, -1, 10, 4),
            Promo("EXHAUSTED", true, -1, 10, 5),
            Promo("NOTSTARTED", true, 1, 10, 0),
            Promo("EXPIRED", true, -10, -1, 0),
            Promo("INACTIVE", false, -1, 10, 0));
        db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    private PromoCodeService Service() => new(new Repository<PromoCode>(_db.NewContext()), _clock);

    private async Task<string[]> CodesAsync(bool? active) =>
        (await Service().SearchAsync(null, active, 1)).Items.Select(p => p.Code).ToArray();

    [Fact]
    public async Task Search_ActiveListsOnlyUsableCodes() =>
        Assert.Equal(["USABLE"], await CodesAsync(true));

    [Fact]
    public async Task Search_InactiveListsEveryOtherState() =>
        Assert.Equal(["EXHAUSTED", "EXPIRED", "INACTIVE", "NOTSTARTED"], await CodesAsync(false));

    [Fact]
    public async Task Search_NullListsAll() =>
        Assert.Equal(["EXHAUSTED", "EXPIRED", "INACTIVE", "NOTSTARTED", "USABLE"], await CodesAsync(null));
}
