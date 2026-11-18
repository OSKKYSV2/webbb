using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Domain.Repositories;

public interface IServiceTypeRepository
{
    Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default);
    Task<ServiceType?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> IsUsedAsync(int id, CancellationToken ct = default);
    Task AddAsync(ServiceType serviceType, CancellationToken ct = default);
    void Remove(ServiceType serviceType);
}
