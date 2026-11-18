using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Domain.Repositories;

public interface IManufacturerRepository
{
    Task<IReadOnlyList<Manufacturer>> GetAllAsync(CancellationToken ct = default);
    Task<Manufacturer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> IsUsedAsync(int id, CancellationToken ct = default);
    Task AddAsync(Manufacturer manufacturer, CancellationToken ct = default);
    void Remove(Manufacturer manufacturer);
}
