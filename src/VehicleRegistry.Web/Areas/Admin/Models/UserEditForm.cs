using System.ComponentModel.DataAnnotations;

namespace VehicleRegistry.Web.Areas.Admin.Models;

public sealed class UserEditForm
{
    public string Id { get; set; } = "";

    [Required, Display(Name = "Jméno"), StringLength(80)]
    public string FirstName { get; set; } = "";

    [Required, Display(Name = "Příjmení"), StringLength(80)]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, Display(Name = "E-mail"), StringLength(256)]
    public string Email { get; set; } = "";

    [Required, Display(Name = "Role")]
    public string Role { get; set; } = "Customer";

    [DataType(DataType.Password), Display(Name = "Počáteční heslo (jen nový uživatel)")]
    public string? Password { get; set; }
}
