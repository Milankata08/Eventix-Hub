using System.ComponentModel.DataAnnotations;
using EventTixHub.Web.Services;

namespace EventTixHub.Web.ViewModels;

public class ProfileIndexVM
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public TicketHistoryFilter Filter { get; set; }
    public PagedResult<UserTicketDto> Tickets { get; set; } = null!;
}

public class ProfileEditVM
{
    [Required(ErrorMessage = "Името е задължително."), StringLength(50, ErrorMessage = "Името може да е до {1} символа."), Display(Name = "Име")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Фамилията е задължителна."), StringLength(50, ErrorMessage = "Фамилията може да е до {1} символа."), Display(Name = "Фамилия")]
    public string LastName { get; set; } = "";

    [Phone(ErrorMessage = "Невалиден телефонен номер."), StringLength(20, ErrorMessage = "Телефонът може да е до {1} символа."), Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }
}
