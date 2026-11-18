using VehicleRegistry.Domain.Enums;

namespace VehicleRegistry.Application.Models;

public sealed record ServiceRecordInput(
    int VehicleId, int ServiceTypeId, int? ServiceCenterId,
    DateTime ServiceDate, int MileageAtService, decimal Price,
    ServiceRecordStatus Status, string? Note);
