namespace VehicleRegistry.Domain.Entities;

/// <summary>Druh servisního úkonu (výměna oleje, STK, pneuservis…).</summary>
public class ServiceType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Doporučený interval v km (null = neurčen).</summary>
    public int? RecommendedIntervalKm { get; set; }
    /// <summary>Doporučený interval v měsících (null = neurčen).</summary>
    public int? RecommendedIntervalMonths { get; set; }

    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
