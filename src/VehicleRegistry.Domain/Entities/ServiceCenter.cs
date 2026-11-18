namespace VehicleRegistry.Domain.Entities;

/// <summary>Servis / autodílna, kde byl úkon proveden.</summary>
public class ServiceCenter : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
