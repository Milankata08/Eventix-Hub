using System.ComponentModel.DataAnnotations;
using System.Globalization;
using EventTixHub.Web.Services;
using QRCoder;

namespace EventTixHub.Web.ViewModels;

public record HallVM(SeatMapDto Map, bool Monitor, bool SalesClosed, CartHoldVM? Hold);

// The buyer's own hold, named in the seat summary: seats for this showtime, or else the whole cart.
// One entry per seat, soonest first, so the page can lower the count as each seat's hold ends.
public record CartHoldVM(bool ForThisShowtime, IReadOnlyList<int> SecondsLeft)
{
    public static CartHoldVM? From(CartDto? cart, int showtimeId, DateTime now)
    {
        var all = cart?.Lines ?? [];
        var here = all.Where(l => l.ShowtimeId == showtimeId).ToList();
        IReadOnlyList<CartLineDto> lines = here.Count > 0 ? here : all;
        return lines.Count == 0 ? null
            : new CartHoldVM(here.Count > 0, lines.Select(l => BookingRules.SecondsLeft(l.ReservedUntil, now)).Order().ToList());
    }
}

public record CartVM(CartDto? Cart, int SecondsLeft);

public record CheckoutVM(CartDto Cart, PaymentVM Payment, int SecondsLeft);

public record HoldBadgeVM(int SecondsLeft, bool Large);

public record TicketCardVM(UserTicketDto Ticket, string? QrDataUri);

public record ConfirmationVM(int OrderId, decimal Total, IReadOnlyList<TicketCardVM> Tickets);

public static class Qr
{
    // PNG data URI, rendered server-side so it works offline.
    public static string? DataUri(string? code)
    {
        if (string.IsNullOrEmpty(code))
            return null;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
        return "data:image/png;base64," + Convert.ToBase64String(new PngByteQRCode(data).GetGraphic(8));
    }
}

// Card is valid through the end of its expiry month. Format errors are left to the regex attribute.
public class NotExpiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (!DateTime.TryParseExact(value as string, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
            return ValidationResult.Success;
        var now = (context.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System).GetLocalNow();
        return expiry.Year * 12 + expiry.Month < now.Year * 12 + now.Month
            ? new ValidationResult("Картата е с изтекла валидност.")
            : ValidationResult.Success;
    }
}

// Simulated payment: validated, then discarded. Never stored or logged.
public class PaymentVM
{
    [Required(ErrorMessage = "Името на картодържателя е задължително."), StringLength(100, ErrorMessage = "Името може да е до {1} символа."), Display(Name = "Картодържател")]
    public string CardholderName { get; set; } = "";

    [Required(ErrorMessage = "Номерът на картата е задължителен."), CreditCard(ErrorMessage = "Невалиден номер на карта."), Display(Name = "Номер на карта")]
    public string CardNumber { get; set; } = "";

    [Required(ErrorMessage = "Валидността е задължителна."), RegularExpression(@"^(0[1-9]|1[0-2])/\d{2}$", ErrorMessage = "Валидността трябва да е във формат ММ/ГГ."), NotExpired, Display(Name = "Валидна до")]
    public string Expiry { get; set; } = "";

    [Required(ErrorMessage = "CVV е задължителен."), RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV трябва да е 3 или 4 цифри."), Display(Name = "CVV")]
    public string Cvv { get; set; } = "";
}
