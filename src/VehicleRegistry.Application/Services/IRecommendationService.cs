using VehicleRegistry.Application.Models;

namespace VehicleRegistry.Application.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<ServiceRecommendation>> ForVehicleAsync(int vehicleId, CancellationToken ct = default);
}
