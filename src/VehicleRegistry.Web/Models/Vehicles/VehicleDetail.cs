using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Web.Models.Vehicles;

public sealed record VehicleDetail(Vehicle Vehicle, IReadOnlyList<ServiceRecommendation> Recommendations,
    bool CanEdit, bool IsStaff);
