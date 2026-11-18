using System.ComponentModel.DataAnnotations;

namespace VehicleRegistry.Web.Models.Account;

public sealed class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "E-mail")]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Heslo")]
    public string Password { get; set; } = "";

    [Display(Name = "Zapamatovat přihlášení")]
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
