using System.ComponentModel.DataAnnotations;

namespace EventTixHub.Web.Models;

// Persisted as int; index filters depend on these values - never reorder.
public enum EventType
{
    [Display(Name = "Филм")] Movie,
    [Display(Name = "Концерт")] Concert,
    [Display(Name = "Театър")] Theatre
}

public enum SeatCategory
{
    [Display(Name = "Стандартно")] Standard,
    [Display(Name = "VIP")] Vip
}

public enum OrderStatus
{
    [Display(Name = "Чакаща")] Pending = 0,
    [Display(Name = "Платена")] Paid = 1,
    [Display(Name = "Отказана")] Cancelled = 2
}

public enum TicketStatus
{
    [Display(Name = "Резервиран")] Reserved = 0,
    [Display(Name = "Платен")] Paid = 1,
    [Display(Name = "Отказан")] Cancelled = 2
}

public enum ConcessionType
{
    [Display(Name = "Редовен")] Regular,
    [Display(Name = "Студент")] Student,
    [Display(Name = "Пенсионер")] Pensioner
}
