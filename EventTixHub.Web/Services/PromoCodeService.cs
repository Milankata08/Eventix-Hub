using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTixHub.Web.Services;

public class PromoCodeService(IRepository<PromoCode> promos, TimeProvider clock)
{
    private DateTime Now => clock.GetLocalNow().DateTime;

    // active: true = active and not expired, false = deactivated or expired, null = all.
    public Task<PagedResult<PromoCode>> SearchAsync(string? search, bool? active, int page, int pageSize = 10)
    {
        var now = Now;
        var query = promos.Query();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            query = query.Where(p => p.Code.Contains(s));
        }
        if (active == true)
            query = query.Where(p => p.IsActive && p.ValidUntil >= now);
        else if (active == false)
            query = query.Where(p => !p.IsActive || p.ValidUntil < now);
        return query.OrderBy(p => p.Code).ToPagedResultAsync(page, pageSize);
    }

    public Task<PromoCode?> GetByIdAsync(int id) => promos.GetByIdAsync(id);

    public async Task<PromoCode> CreateAsync(PromoCode promo)
    {
        promo.Code = promo.Code.Trim().ToUpperInvariant();
        promo.TimesUsed = 0;
        await EnsureCodeFreeAsync(promo);
        await promos.AddAsync(promo);
        await promos.SaveChangesAsync();
        return promo;
    }

    public async Task UpdateAsync(PromoCode promo)
    {
        promo.Code = promo.Code.Trim().ToUpperInvariant();
        var current = await promos.GetByIdAsync(promo.Id)
            ?? throw new BookingException("Промо кодът не е намерен.");
        if (promo.UsageLimit < current.TimesUsed)
            throw new BookingException($"Лимитът не може да е под вече използваните пъти ({current.TimesUsed}).");
        await EnsureCodeFreeAsync(promo);

        current.Code = promo.Code;
        current.DiscountPercent = promo.DiscountPercent;
        current.UsageLimit = promo.UsageLimit;
        current.ValidFrom = promo.ValidFrom;
        current.ValidUntil = promo.ValidUntil;
        current.IsActive = promo.IsActive;
        await promos.SaveChangesAsync();
    }

    // BR-15
    public async Task DeleteAsync(int id)
    {
        var promo = await promos.GetByIdAsync(id)
            ?? throw new BookingException("Промо кодът не е намерен.");
        if (promo.TimesUsed > 0)
            throw new BookingException("Използван промо код не може да се изтрие. Деактивирайте го.");
        promos.Remove(promo);
        await promos.SaveChangesAsync();
    }

    private async Task EnsureCodeFreeAsync(PromoCode promo)
    {
        if (await promos.Query().AnyAsync(p => p.Code == promo.Code && p.Id != promo.Id))
            throw new BookingException("Вече има промо код с това име.");
    }
}
