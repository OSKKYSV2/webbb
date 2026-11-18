using System.ComponentModel.DataAnnotations;
using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Web.Validation;

namespace VehicleRegistry.Web.Models.ServiceRecords;

public sealed class ServiceRecordForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Vozidlo")]
    public int VehicleId { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Typ úkonu")]
    public int ServiceTypeId { get; set; }

    [Display(Name = "Servis / dílna")]
    public int? ServiceCenterId { get; set; }

    [Required, DataType(DataType.Date), NotInFuture, Display(Name = "Datum servisu")]
    public DateTime ServiceDate { get; set; } = DateTime.Today;

    [Range(0, 5_000_000), Display(Name = "Tachometr (km)")]
    public int MileageAtService { get; set; }

    [Range(typeof(decimal), "0", "99999999"), Display(Name = "Cena (Kč)")]
    public decimal Price { get; set; }

    [Display(Name = "Stav")]
    public ServiceRecordStatus Status { get; set; } = ServiceRecordStatus.Completed;

    [StringLength(1000), Display(Name = "Poznámka")]
    public string? Note { get; set; }

    public ServiceRecordInput ToInput() => new(VehicleId, ServiceTypeId, ServiceCenterId,
        ServiceDate, MileageAtService, Price, Status, Note);
}
