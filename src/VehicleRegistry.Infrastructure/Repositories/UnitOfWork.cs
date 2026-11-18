using VehicleRegistry.Domain.Repositories;
using VehicleRegistry.Infrastructure.Persistence;

namespace VehicleRegistry.Infrastructure.Repositories;

public sealed class UnitOfWork(
    ApplicationDbContext db,
    IVehicleRepository vehicles,
    IServiceRecordRepository records,
    IManufacturerRepository manufacturers,
    IServiceTypeRepository types,
    IServiceCenterRepository centers) : IUnitOfWork
{
    public IVehicleRepository Vehicles { get; } = vehicles;
    public IServiceRecordRepository ServiceRecords { get; } = records;
    public IManufacturerRepository Manufacturers { get; } = manufacturers;
    public IServiceTypeRepository ServiceTypes { get; } = types;
    public IServiceCenterRepository ServiceCenters { get; } = centers;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
