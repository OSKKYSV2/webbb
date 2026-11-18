using VehicleRegistry.Domain.Enums;

namespace VehicleRegistry.Domain.Entities;

/// <summary>Záznam servisního úkonu provedeného na vozidle.</summary>
public class ServiceRecord : BaseEntity
{
    public DateTime ServiceDate { get; set; }
    public int MileageAtService { get; set; }
    public decimal Price { get; set; }
    public string? Note { get; set; }
    public ServiceRecordStatus Status { get; set; } = ServiceRecordStatus.Completed;

    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public int ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }

    public int? ServiceCenterId { get; set; }
    public ServiceCenter? ServiceCenter { get; set; }

    /// <summary>Id uživatele (manager/admin), který záznam zapsal.</summary>
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}
