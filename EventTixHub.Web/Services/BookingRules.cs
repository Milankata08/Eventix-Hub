using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using EventTixHub.Web.Models;

namespace EventTixHub.Web.Services;

public enum SeatStatus { Free, Held, Sold }

// Computed, never persisted.
public enum PromoState
{
    [Display(Name = "Неактивен")] Inactive,
    [Display(Name = "Изтекъл")] Expired,
    [Display(Name = "Предстоящ")] NotStarted,
    [Display(Name = "Изчерпан")] Exhausted,
    [Display(Name = "Активен")] Usable
}

public static class BookingRules
{
    public const int HoldMinutes = 10;
    public const int CancellationDeadlineHours = 24;
    public const int MaxTicketsPerOrder = 10;
    public const int StudentPercent = 20;
    public const int PensionerPercent = 30;
    public const int CriticalOccupancyPercent = 90;

    public static decimal TicketPrice(decimal standardPrice, decimal vipPrice, SeatCategory category,
        ConcessionType concession, int promoPercent)
    {
        var basePrice = category == SeatCategory.Vip ? vipPrice : standardPrice;
        var concessionPercent = concession switch
        {
            ConcessionType.Student => StudentPercent,
            ConcessionType.Pensioner => PensionerPercent,
            _ => 0
        };
        var price = basePrice * (100 - concessionPercent) / 100m * (100 - promoPercent) / 100m;
        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal OrderTotal(IEnumerable<decimal> ticketPrices) => ticketPrices.Sum();

    // status = null means no ticket exists for the seat.
    public static SeatStatus StatusOf(TicketStatus? status, DateTime? reservedUntil, DateTime now) => status switch
    {
        TicketStatus.Paid => SeatStatus.Sold,
        TicketStatus.Reserved when reservedUntil > now => SeatStatus.Held,
        _ => SeatStatus.Free
    };

    // EF-translatable forms of BR-01/BR-03. StatusOf stays the in-memory form; BookingRulesTests proves they agree.
    public static Expression<Func<Ticket, bool>> LiveHold(DateTime now) =>
        t => t.Status == TicketStatus.Reserved && t.ReservedUntil > now;

    public static Expression<Func<Ticket, bool>> ExpiredHold(DateTime now) =>
        t => t.Status == TicketStatus.Reserved && t.ReservedUntil <= now;

    // Paid or live hold: the seat is not free.
    public static Expression<Func<Ticket, bool>> Taken(DateTime now) =>
        t => t.Status == TicketStatus.Paid || (t.Status == TicketStatus.Reserved && t.ReservedUntil > now);

    public static bool CanCancel(DateTime startsAt, DateTime now) =>
        startsAt - now >= TimeSpan.FromHours(CancellationDeadlineHours);

    // First matching state wins: an inactive expired code is Inactive, an expired exhausted one is Expired.
    public static PromoState StateOf(PromoCode p, DateTime now) =>
        !p.IsActive ? PromoState.Inactive
        : now > p.ValidUntil ? PromoState.Expired
        : now < p.ValidFrom ? PromoState.NotStarted
        : p.TimesUsed >= p.UsageLimit ? PromoState.Exhausted
        : PromoState.Usable;

    public static bool IsPromoUsable(PromoCode promo, DateTime now) => StateOf(promo, now) == PromoState.Usable;

    // EF-translatable form of IsPromoUsable; BookingRulesTests proves they agree.
    public static Expression<Func<PromoCode, bool>> UsablePromo(DateTime now) =>
        p => p.IsActive && p.ValidFrom <= now && now <= p.ValidUntil && p.TimesUsed < p.UsageLimit;

    // Half-open intervals: back-to-back showtimes do not overlap.
    public static bool Overlaps(DateTime startA, int durationMinutesA, DateTime startB, int durationMinutesB) =>
        startA < startB.AddMinutes(durationMinutesB) && startB < startA.AddMinutes(durationMinutesA);
}
