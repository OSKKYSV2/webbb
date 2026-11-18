using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Application.Services;

public interface IVehicleService
{
    Task<IReadOnlyList<Vehicle>> GetMineAsync(string ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> SearchAsync(string query, CancellationToken ct = default);
    Task<Vehicle?> GetVisibleAsync(int id, string userId, bool staff, CancellationToken ct = default);
    Task<Vehicle> CreateAsync(VehicleInput input, string ownerId, CancellationToken ct = default);
    Task UpdateAsync(int id, VehicleInput input, string userId, bool admin, CancellationToken ct = default);
    Task UpdatePhotoAsync(int id, string? path, string userId, bool admin, CancellationToken ct = default);
    Task DeleteAsync(int id, string userId, bool admin, CancellationToken ct = default);
}
