namespace VehicleRegistry.Domain.Repositories;

public interface IUnitOfWork
{
    IVehicleRepository Vehicles { get; }
    IServiceRecordRepository ServiceRecords { get; }
    IManufacturerRepository Manufacturers { get; }
    IServiceTypeRepository ServiceTypes { get; }
    IServiceCenterRepository ServiceCenters { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
