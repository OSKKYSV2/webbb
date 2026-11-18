using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Domain.Repositories;

public interface IServiceCenterRepository
{
    Task<IReadOnlyList<ServiceCenter>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceCenter?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> IsUsedAsync(int id, CancellationToken ct = default);
    Task AddAsync(ServiceCenter serviceCenter, CancellationToken ct = default);
    void Remove(ServiceCenter serviceCenter);
}
