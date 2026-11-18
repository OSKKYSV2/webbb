using VehicleRegistry.Domain.Enums;

namespace VehicleRegistry.Application.Models;

public sealed record VehicleInput(
    string Vin, string LicensePlate, string Model, int YearOfManufacture,
    FuelType FuelType, int Mileage, string? Color, int ManufacturerId);
