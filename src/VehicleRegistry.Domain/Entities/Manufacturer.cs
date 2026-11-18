namespace VehicleRegistry.Domain.Entities;

public class Manufacturer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
