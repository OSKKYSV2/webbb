using Microsoft.AspNetCore.Identity;

namespace VehicleRegistry.Domain.Entities;

// Identity account is kept in Domain so a Vehicle can have a typed Owner navigation.
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<ServiceRecord> CreatedServiceRecords { get; set; } = new List<ServiceRecord>();
}
