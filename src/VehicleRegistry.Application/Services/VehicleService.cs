using System.Text.RegularExpressions;
using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Domain.Repositories;

namespace VehicleRegistry.Application.Services;

public sealed class VehicleService(IUnitOfWork uow) : IVehicleService
{
    public Task<IReadOnlyList<Vehicle>> GetMineAsync(string ownerId, CancellationToken ct = default) =>
        uow.Vehicles.GetByOwnerAsync(ownerId, ct);

    public Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken ct = default) =>
        uow.Vehicles.GetAllAsync(ct);

    public Task<IReadOnlyList<Vehicle>> SearchAsync(string query, CancellationToken ct = default) =>
        uow.Vehicles.SearchAsync(query.Trim(), ct);

    public async Task<Vehicle?> GetVisibleAsync(int id, string userId, bool staff, CancellationToken ct = default)
    {
        var vehicle = await uow.Vehicles.GetWithDetailsAsync(id, ct);
        return vehicle is not null && (staff || vehicle.OwnerId == userId) ? vehicle : null;
    }

    public async Task<Vehicle> CreateAsync(VehicleInput input, string ownerId, CancellationToken ct = default)
    {
        await ValidateAsync(input, null, ct);
        if (string.IsNullOrWhiteSpace(ownerId)) throw new BusinessRuleException("Vlastník vozidla není určen.");
        var vehicle = new Vehicle { OwnerId = ownerId };
        Apply(input, vehicle);
        await uow.Vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        return vehicle;
    }

    public async Task UpdateAsync(int id, VehicleInput input, string userId, bool admin, CancellationToken ct = default)
    {
        var vehicle = await AllowedAsync(id, userId, admin, ct);
        await ValidateAsync(input, id, ct);
        var lastMileage = vehicle.ServiceRecords.Where(r => r.Status == Domain.Enums.ServiceRecordStatus.Completed)
            .Select(r => r.MileageAtService).DefaultIfEmpty(0).Max();
        if (input.Mileage < lastMileage)
            throw new BusinessRuleException("Tachometr nemůže být menší než u dokončeného servisního záznamu.");
        Apply(input, vehicle);
        await uow.SaveChangesAsync(ct);
    }

    public async Task UpdatePhotoAsync(int id, string? path, string userId, bool admin, CancellationToken ct = default)
    {
        var vehicle = await AllowedAsync(id, userId, admin, ct);
        vehicle.ImagePath = path;
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, string userId, bool admin, CancellationToken ct = default)
    {
        var vehicle = await AllowedAsync(id, userId, admin, ct);
        uow.Vehicles.Remove(vehicle);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Vehicle> AllowedAsync(int id, string userId, bool admin, CancellationToken ct)
    {
        var vehicle = await uow.Vehicles.GetWithDetailsAsync(id, ct);
        if (vehicle is null || (!admin && vehicle.OwnerId != userId))
            throw new BusinessRuleException("Vozidlo nebylo nalezeno nebo k němu nemáte přístup.");
        return vehicle;
    }

    private async Task ValidateAsync(VehicleInput input, int? id, CancellationToken ct)
    {
        var vin = input.Vin.Trim().ToUpperInvariant();
        var plate = input.LicensePlate.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(vin, "^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.CultureInvariant))
            throw new BusinessRuleException("VIN musí mít 17 znaků, bez I, O a Q.");
        if (plate.Length is < 1 or > 10) throw new BusinessRuleException("SPZ musí mít 1–10 znaků.");
        if (string.IsNullOrWhiteSpace(input.Model) || input.Model.Trim().Length > 100)
            throw new BusinessRuleException("Vyplňte model (max. 100 znaků).");
        if (input.YearOfManufacture < 1900 || input.YearOfManufacture > DateTime.Today.Year + 1)
            throw new BusinessRuleException("Neplatný rok výroby.");
        if (input.Mileage < 0) throw new BusinessRuleException("Stav tachometru nesmí být záporný.");
        if (!Enum.IsDefined(input.FuelType)) throw new BusinessRuleException("Neplatný druh paliva.");
        if (input.Color?.Length > 50) throw new BusinessRuleException("Barva může mít maximálně 50 znaků.");
        if (await uow.Manufacturers.GetByIdAsync(input.ManufacturerId, ct) is null)
            throw new BusinessRuleException("Vyberte platného výrobce.");
        if (await uow.Vehicles.VinInUseAsync(vin, id, ct))
            throw new BusinessRuleException("Toto VIN už je evidováno.");
        if (await uow.Vehicles.PlateInUseAsync(plate, id, ct))
            throw new BusinessRuleException("Tato SPZ už je evidována.");
    }

    private static void Apply(VehicleInput input, Vehicle vehicle)
    {
        vehicle.Vin = input.Vin.Trim().ToUpperInvariant();
        vehicle.LicensePlate = input.LicensePlate.Trim().ToUpperInvariant();
        vehicle.Model = input.Model.Trim();
        vehicle.YearOfManufacture = input.YearOfManufacture;
        vehicle.FuelType = input.FuelType;
        vehicle.Mileage = input.Mileage;
        vehicle.Color = input.Color?.Trim();
        vehicle.ManufacturerId = input.ManufacturerId;
    }
}
