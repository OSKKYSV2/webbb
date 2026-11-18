using System.ComponentModel.DataAnnotations;

namespace VehicleRegistry.Web.Models.Account;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Vyplňte jméno."), StringLength(80)]
    [Display(Name = "Jméno")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Vyplňte příjmení."), StringLength(80)]
    [Display(Name = "Příjmení")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Vyplňte e-mail."), EmailAddress, StringLength(256)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vyplňte heslo."), DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Hesla se neshodují.")]
    [Display(Name = "Potvrzení hesla")]
    public string ConfirmPassword { get; set; } = "";
}
