using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Domain.Repositories;
using VehicleRegistry.Infrastructure.Persistence;

namespace VehicleRegistry.Infrastructure.Repositories;

public sealed class VehicleRepository(ApplicationDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Vehicle?> GetWithDetailsAsync(int id, CancellationToken ct = default) =>
        db.Vehicles.Include(v => v.Owner).Include(v => v.Manufacturer)
            .Include(v => v.ServiceRecords).ThenInclude(r => r.ServiceType)
            .Include(v => v.ServiceRecords).ThenInclude(r => r.ServiceCenter)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Vehicle>> GetByOwnerAsync(string ownerId, CancellationToken ct = default) =>
        await db.Vehicles.AsNoTracking().Include(v => v.Manufacturer)
            .Where(v => v.OwnerId == ownerId).OrderBy(v => v.LicensePlate).ToListAsync(ct);

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default) =>
        await db.Vehicles.AsNoTracking().Include(v => v.Manufacturer).Include(v => v.Owner)
            .OrderBy(v => v.LicensePlate).ToListAsync(ct);

    public async Task<IReadOnlyList<Vehicle>> SearchAsync(string search, CancellationToken ct = default) =>
        await db.Vehicles.AsNoTracking().Include(v => v.Manufacturer)
            .Where(v => v.LicensePlate.Contains(search) || v.Vin.Contains(search))
            .OrderBy(v => v.LicensePlate).Take(100).ToListAsync(ct);

    public Task<bool> VinInUseAsync(string vin, int? exceptId = null, CancellationToken ct = default) =>
        db.Vehicles.AnyAsync(v => v.Vin == vin && (!exceptId.HasValue || v.Id != exceptId.Value), ct);

    public Task<bool> PlateInUseAsync(string plate, int? exceptId = null, CancellationToken ct = default) =>
        db.Vehicles.AnyAsync(v => v.LicensePlate == plate && (!exceptId.HasValue || v.Id != exceptId.Value), ct);

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct = default) =>
        await db.Vehicles.AddAsync(vehicle, ct);
    public void Remove(Vehicle vehicle) => db.Vehicles.Remove(vehicle);
}

public sealed class ServiceRecordRepository(ApplicationDbContext db) : IServiceRecordRepository
{
    public Task<ServiceRecord?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.ServiceRecords.Include(r => r.Vehicle).Include(r => r.ServiceType)
            .Include(r => r.ServiceCenter).Include(r => r.CreatedBy).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ServiceRecord>> GetByVehicleAsync(int vehicleId, CancellationToken ct = default) =>
        await db.ServiceRecords.AsNoTracking().Include(r => r.ServiceType).Include(r => r.ServiceCenter)
            .Include(r => r.CreatedBy)
            .Where(r => r.VehicleId == vehicleId).OrderByDescending(r => r.ServiceDate).ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceRecord>> GetAllAsync(CancellationToken ct = default) =>
        await db.ServiceRecords.AsNoTracking().Include(r => r.Vehicle).Include(r => r.ServiceType)
            .Include(r => r.ServiceCenter).Include(r => r.CreatedBy).OrderByDescending(r => r.ServiceDate).ToListAsync(ct);

    public async Task AddAsync(ServiceRecord record, CancellationToken ct = default) =>
        await db.ServiceRecords.AddAsync(record, ct);
    public void Remove(ServiceRecord record) => db.ServiceRecords.Remove(record);
}

public sealed class ManufacturerRepository(ApplicationDbContext db) : IManufacturerRepository
{
    public async Task<IReadOnlyList<Manufacturer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Manufacturers.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct);
    public Task<Manufacturer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Manufacturers.FirstOrDefaultAsync(m => m.Id == id, ct);
    public Task<bool> IsUsedAsync(int id, CancellationToken ct = default) =>
        db.Vehicles.AnyAsync(v => v.ManufacturerId == id, ct);
    public async Task AddAsync(Manufacturer m, CancellationToken ct = default) => await db.Manufacturers.AddAsync(m, ct);
    public void Remove(Manufacturer m) => db.Manufacturers.Remove(m);
}

public sealed class ServiceTypeRepository(ApplicationDbContext db) : IServiceTypeRepository
{
    public async Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default) =>
        await db.ServiceTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
    public Task<ServiceType?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.ServiceTypes.FirstOrDefaultAsync(t => t.Id == id, ct);
    public Task<bool> IsUsedAsync(int id, CancellationToken ct = default) =>
        db.ServiceRecords.AnyAsync(r => r.ServiceTypeId == id, ct);
    public async Task AddAsync(ServiceType t, CancellationToken ct = default) => await db.ServiceTypes.AddAsync(t, ct);
    public void Remove(ServiceType t) => db.ServiceTypes.Remove(t);
}

public sealed class ServiceCenterRepository(ApplicationDbContext db) : IServiceCenterRepository
{
    public async Task<IReadOnlyList<ServiceCenter>> GetAllAsync(CancellationToken ct = default) =>
        await db.ServiceCenters.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
    public Task<ServiceCenter?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.ServiceCenters.FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<bool> IsUsedAsync(int id, CancellationToken ct = default) =>
        db.ServiceRecords.AnyAsync(r => r.ServiceCenterId == id, ct);
    public async Task AddAsync(ServiceCenter c, CancellationToken ct = default) => await db.ServiceCenters.AddAsync(c, ct);
    public void Remove(ServiceCenter c) => db.ServiceCenters.Remove(c);
}
