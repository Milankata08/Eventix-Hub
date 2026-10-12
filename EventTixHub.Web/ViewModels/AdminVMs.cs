using System.ComponentModel.DataAnnotations;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EventTixHub.Web.ViewModels;

public record DeleteVM(int Id, string Title, string Details);

public record AdminDashboardVM(DashboardDto Kpis, IReadOnlyList<ShowtimeRowDto> Upcoming);

public class EventFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Заглавието е задължително."), StringLength(150, ErrorMessage = "Заглавието може да е до {1} символа."), Display(Name = "Заглавие")]
    public string Title { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Описанието може да е до {1} символа."), Display(Name = "Описание")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Изберете вид."), Display(Name = "Вид")]
    public EventType Type { get; set; }

    [Required(ErrorMessage = "Жанрът е задължителен."), StringLength(50, ErrorMessage = "Жанрът може да е до {1} символа."), Display(Name = "Жанр")]
    public string Genre { get; set; } = "";

    [Required(ErrorMessage = "Продължителността е задължителна."), Range(1, 600, ErrorMessage = "Продължителността трябва да е между {1} и {2} минути."), Display(Name = "Продължителност (мин)")]
    public int DurationMinutes { get; set; } = 90;

    /// <summary>Current poster URL, display only: set by the server, never bound from the form.</summary>
    [BindNever]
    public string? PosterUrl { get; set; }

    [Display(Name = "Премахни плаката")]
    public bool RemovePoster { get; set; }

    [Display(Name = "Плакат (JPG/PNG, до 2 MB)")]
    public IFormFile? Poster { get; set; }

    public static EventFormVM From(Event e) => new()
    {
        Id = e.Id, Title = e.Title, Description = e.Description, Type = e.Type,
        Genre = e.Genre, DurationMinutes = e.DurationMinutes, PosterUrl = e.PosterUrl
    };

    public Event ToEntity() => new()
    {
        Id = Id, Title = Title.Trim(), Description = Description, Type = Type, Genre = Genre.Trim(),
        DurationMinutes = DurationMinutes, PosterUrl = PosterUrl
    };
}

public class VenueFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Името е задължително."), StringLength(100, ErrorMessage = "Името може да е до {1} символа."), Display(Name = "Име")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Градът е задължителен."), StringLength(50, ErrorMessage = "Градът може да е до {1} символа."), Display(Name = "Град")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Адресът е задължителен."), StringLength(200, ErrorMessage = "Адресът може да е до {1} символа."), Display(Name = "Адрес")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "Редовете са задължителни."), Range(1, 30, ErrorMessage = "Редовете трябва да са между {1} и {2}."), Display(Name = "Редове")]
    public int Rows { get; set; } = 8;

    [Required(ErrorMessage = "Местата в ред са задължителни."), Range(1, 40, ErrorMessage = "Местата в ред трябва да са между {1} и {2}."), Display(Name = "Места в ред")]
    public int SeatsPerRow { get; set; } = 10;

    public static VenueFormVM From(Venue v) => new()
    {
        Id = v.Id, Name = v.Name, City = v.City, Address = v.Address, Rows = v.Rows, SeatsPerRow = v.SeatsPerRow
    };

    public Venue ToEntity() => new()
    {
        Id = Id, Name = Name.Trim(), City = City.Trim(), Address = Address.Trim(), Rows = Rows, SeatsPerRow = SeatsPerRow
    };
}

public record AdminEventsVM(PagedResult<EventCardDto> Events, FilterOptions Options, string? Search, EventType? Type, string? Genre);

public record AdminEventDetailsVM(Event Event, IReadOnlyList<ShowtimeRowDto> Showtimes);

public record AdminShowtimesVM(
    PagedResult<ShowtimeRowDto> Showtimes, IReadOnlyList<EventOption> Events, IReadOnlyList<Venue> Venues,
    ShowtimeFilter Filter);

public record AdminVenuesVM(PagedResult<Venue> Venues, IReadOnlyList<string> Cities, string? Search, string? City);

public record VenueDetailsVM(Venue Venue, IReadOnlyList<Seat> Seats);

public record AdminPromoCodesVM(PagedResult<PromoCode> Promos, string? Search, bool? Active, DateTime Now)
{
    public PromoState StateOf(PromoCode p) => BookingRules.StateOf(p, Now);
}

public record AdminUsersVM(PagedResult<UserRowDto> Users, string? Search, string? Role, int CurrentUserId);

public class ShowtimeFormVM : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Изберете събитие."), Range(1, int.MaxValue, ErrorMessage = "Изберете събитие."), Display(Name = "Събитие")]
    public int EventId { get; set; }

    [Required(ErrorMessage = "Изберете зала."), Range(1, int.MaxValue, ErrorMessage = "Изберете зала."), Display(Name = "Зала")]
    public int VenueId { get; set; }

    [Required(ErrorMessage = "Началото е задължително."), Display(Name = "Начало")]
    public DateTime? StartsAt { get; set; }

    [Required(ErrorMessage = "Стандартната цена е задължителна."), Range(typeof(decimal), "0.01", "1000", ErrorMessage = "Цената трябва да е между {1} и {2} €."), Display(Name = "Стандартна цена (€)")]
    public decimal? StandardPrice { get; set; }

    [Required(ErrorMessage = "VIP цената е задължителна."), Range(typeof(decimal), "0.01", "1000", ErrorMessage = "Цената трябва да е между {1} и {2} €."), Display(Name = "VIP цена (€)")]
    public decimal? VipPrice { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<EventOption> Events { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<VenueOption> Venues { get; set; } = [];

    public static ShowtimeFormVM From(Showtime s) => new()
    {
        Id = s.Id, EventId = s.EventId, VenueId = s.VenueId, StartsAt = s.StartsAt,
        StandardPrice = s.StandardPrice, VipPrice = s.VipPrice
    };

    public Showtime ToEntity() => new()
    {
        Id = Id, EventId = EventId, VenueId = VenueId, StartsAt = StartsAt!.Value,
        StandardPrice = StandardPrice!.Value, VipPrice = VipPrice!.Value
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (VipPrice < StandardPrice)
            yield return new ValidationResult("VIP цената не може да е под стандартната.", [nameof(VipPrice)]);
        var clock = context.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;
        // An existing showtime may keep its start; the service re-checks when it changes.
        if (Id == 0 && StartsAt is { } start && start <= clock.GetLocalNow().DateTime)
            yield return new ValidationResult("Прожекцията трябва да започва в бъдещето.", [nameof(StartsAt)]);
    }
}

public class PromoCodeFormVM : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Кодът е задължителен."), StringLength(20, MinimumLength = 3, ErrorMessage = "Кодът трябва да е между {2} и {1} символа."),
     RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Кодът може да съдържа само букви и цифри."), Display(Name = "Код")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Отстъпката е задължителна."), Range(1, 100, ErrorMessage = "Отстъпката трябва да е между {1} и {2} %."), Display(Name = "Отстъпка (%)")]
    public int DiscountPercent { get; set; } = 10;

    [Required(ErrorMessage = "Лимитът е задължителен."), Range(1, int.MaxValue, ErrorMessage = "Лимитът трябва да е поне {1}."), Display(Name = "Лимит на използване")]
    public int UsageLimit { get; set; } = 100;

    // Shown on edit; the service re-checks the stored value.
    public int TimesUsed { get; set; }

    [Required(ErrorMessage = "Началото е задължително."), Display(Name = "Валиден от")]
    public DateTime? ValidFrom { get; set; }

    [Required(ErrorMessage = "Краят е задължителен."), Display(Name = "Валиден до")]
    public DateTime? ValidUntil { get; set; }

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;

    public static PromoCodeFormVM From(PromoCode p) => new()
    {
        Id = p.Id, Code = p.Code, DiscountPercent = p.DiscountPercent, UsageLimit = p.UsageLimit,
        TimesUsed = p.TimesUsed, ValidFrom = p.ValidFrom, ValidUntil = p.ValidUntil, IsActive = p.IsActive
    };

    public PromoCode ToEntity() => new()
    {
        Id = Id, Code = Code, DiscountPercent = DiscountPercent, UsageLimit = UsageLimit,
        ValidFrom = ValidFrom!.Value, ValidUntil = ValidUntil!.Value, IsActive = IsActive
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (ValidFrom is { } from && ValidUntil is { } until && until <= from)
            yield return new ValidationResult("Крайната дата трябва да е след началната.", [nameof(ValidUntil)]);
        if (UsageLimit < TimesUsed)
            yield return new ValidationResult($"Лимитът не може да е под вече използваните пъти ({TimesUsed}).", [nameof(UsageLimit)]);
    }
}
