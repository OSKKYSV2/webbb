using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Application.Services;

public interface IServiceHistoryService
{
    Task<IReadOnlyList<ServiceRecord>> ForVehicleAsync(int vehicleId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceRecord>> AllAsync(CancellationToken ct = default);
    Task<ServiceRecord?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(ServiceRecordInput input, string actorId, CancellationToken ct = default);
    Task UpdateAsync(int id, ServiceRecordInput input, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
