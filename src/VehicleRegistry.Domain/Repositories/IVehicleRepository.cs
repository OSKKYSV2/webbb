using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Domain.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Vehicle?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> GetByOwnerAsync(string ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> SearchAsync(string search, CancellationToken ct = default);
    Task<bool> VinInUseAsync(string vin, int? exceptId = null, CancellationToken ct = default);
    Task<bool> PlateInUseAsync(string plate, int? exceptId = null, CancellationToken ct = default);
    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);
    void Remove(Vehicle vehicle);
}
