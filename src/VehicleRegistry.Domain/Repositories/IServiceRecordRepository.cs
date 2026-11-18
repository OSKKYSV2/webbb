using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Domain.Repositories;

public interface IServiceRecordRepository
{
    Task<ServiceRecord?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRecord>> GetByVehicleAsync(int vehicleId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRecord>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(ServiceRecord record, CancellationToken ct = default);
    void Remove(ServiceRecord record);
}
