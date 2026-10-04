using System.ComponentModel.DataAnnotations;

namespace EventTixHub.Web.ViewModels;

public class RegisterVM
{
    [Required(ErrorMessage = "Името е задължително."), StringLength(50, ErrorMessage = "Името може да е до {1} символа."), Display(Name = "Име")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Фамилията е задължителна."), StringLength(50, ErrorMessage = "Фамилията може да е до {1} символа."), Display(Name = "Фамилия")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Имейлът е задължителен."), EmailAddress(ErrorMessage = "Невалиден имейл."), StringLength(256, ErrorMessage = "Имейлът може да е до {1} символа."), Display(Name = "Имейл")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Паролата е задължителна."), StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е между {2} и {1} символа."), DataType(DataType.Password), Display(Name = "Парола")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Потвърдете паролата."), Compare(nameof(Password), ErrorMessage = "Паролите не съвпадат."), DataType(DataType.Password), Display(Name = "Потвърждение на паролата")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginVM
{
    [Required(ErrorMessage = "Имейлът е задължителен."), EmailAddress(ErrorMessage = "Невалиден имейл."), Display(Name = "Имейл")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Паролата е задължителна."), DataType(DataType.Password), Display(Name = "Парола")]
    public string Password { get; set; } = "";

    [Display(Name = "Запомни ме")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
