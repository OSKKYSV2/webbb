namespace VehicleRegistry.Application.Models;

public sealed record ServiceRecommendation(
    string ServiceName, DateTime? LastServiceDate, int? NextMileage,
    DateTime? NextDate, bool Due, bool HasHistory);
