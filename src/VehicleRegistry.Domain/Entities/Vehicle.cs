using VehicleRegistry.Domain.Enums;

namespace VehicleRegistry.Domain.Entities;

public class Vehicle : BaseEntity
{
    /// <summary>VIN – 17 znaků, unikátní.</summary>
    public string Vin { get; set; } = string.Empty;
    /// <summary>SPZ / registrační značka.</summary>
    public string LicensePlate { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int YearOfManufacture { get; set; }
    public FuelType FuelType { get; set; }
    public int Mileage { get; set; }
    public string? Color { get; set; }
    public string? ImagePath { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    /// <summary>Id vlastníka (uživatel z Identity).</summary>
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
