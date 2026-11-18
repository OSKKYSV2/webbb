using System.ComponentModel.DataAnnotations;
using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Web.Validation;

namespace VehicleRegistry.Web.Models.Vehicles;

public sealed class VehicleForm
{
    public int Id { get; set; }

    [Required, Vin, Display(Name = "VIN (17 znaků)")]
    public string Vin { get; set; } = "";

    [Required, StringLength(10, MinimumLength = 1), Display(Name = "Registrační značka")]
    public string LicensePlate { get; set; } = "";

    [Required, StringLength(100), Display(Name = "Model vozidla")]
    public string VehicleModel { get; set; } = "";

    [Range(1900, 2200), Display(Name = "Rok výroby")]
    public int YearOfManufacture { get; set; } = DateTime.Today.Year;

    [Display(Name = "Palivo")]
    public FuelType FuelType { get; set; }

    [Range(0, 5_000_000), Display(Name = "Tachometr (km)")]
    public int Mileage { get; set; }

    [StringLength(50), Display(Name = "Barva")]
    public string? Color { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Výrobce")]
    public int ManufacturerId { get; set; }

    [Display(Name = "Vlastník (pouze administrátor)")]
    public string? OwnerId { get; set; }

    public VehicleInput ToInput() => new(Vin, LicensePlate, VehicleModel, YearOfManufacture,
        FuelType, Mileage, Color, ManufacturerId);
}
