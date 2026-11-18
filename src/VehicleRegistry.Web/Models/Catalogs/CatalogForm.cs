using System.ComponentModel.DataAnnotations;

namespace VehicleRegistry.Web.Models.Catalogs;

public sealed class CatalogForm
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Název")]
    public string Name { get; set; } = "";
    [StringLength(100), Display(Name = "Země")]
    public string? Country { get; set; }
    [StringLength(250), Display(Name = "Adresa")]
    public string? Address { get; set; }
    [StringLength(20), Display(Name = "Telefon")]
    public string? Phone { get; set; }
    [EmailAddress, StringLength(150), Display(Name = "E-mail")]
    public string? Email { get; set; }
    [StringLength(500), Display(Name = "Popis")]
    public string? Description { get; set; }
    [Range(1, 1_000_000), Display(Name = "Interval km")]
    public int? RecommendedIntervalKm { get; set; }
    [Range(1, 1000), Display(Name = "Interval měsíců")]
    public int? RecommendedIntervalMonths { get; set; }
}
