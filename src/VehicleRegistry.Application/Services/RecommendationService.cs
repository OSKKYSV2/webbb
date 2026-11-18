using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Domain.Repositories;

namespace VehicleRegistry.Application.Services;

public sealed class RecommendationService(IUnitOfWork uow) : IRecommendationService
{
    public async Task<IReadOnlyList<ServiceRecommendation>> ForVehicleAsync(int vehicleId, CancellationToken ct = default)
    {
        var vehicle = await uow.Vehicles.GetWithDetailsAsync(vehicleId, ct)
            ?? throw new BusinessRuleException("Vozidlo nebylo nalezeno.");
        var types = await uow.ServiceTypes.GetAllAsync(ct);
        return types.Where(t => t.RecommendedIntervalKm.HasValue || t.RecommendedIntervalMonths.HasValue)
            .Select(type =>
            {
                var latest = vehicle.ServiceRecords
                    .Where(r => r.ServiceTypeId == type.Id && r.Status == ServiceRecordStatus.Completed)
                    .OrderByDescending(r => r.ServiceDate).FirstOrDefault();
                var nextMileage = latest is null || !type.RecommendedIntervalKm.HasValue
                    ? (int?)null : latest.MileageAtService + type.RecommendedIntervalKm.Value;
                var nextDate = latest is null || !type.RecommendedIntervalMonths.HasValue
                    ? (DateTime?)null : latest.ServiceDate.AddMonths(type.RecommendedIntervalMonths.Value);
                var due = nextMileage <= vehicle.Mileage || nextDate?.Date <= DateTime.Today;
                return new ServiceRecommendation(type.Name, latest?.ServiceDate, nextMileage, nextDate, due, latest is not null);
            }).ToArray();
    }
}
