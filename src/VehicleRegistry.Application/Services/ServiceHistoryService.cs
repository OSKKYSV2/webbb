using VehicleRegistry.Application.Models;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Domain.Repositories;

namespace VehicleRegistry.Application.Services;

public sealed class ServiceHistoryService(IUnitOfWork uow) : IServiceHistoryService
{
    public Task<IReadOnlyList<ServiceRecord>> ForVehicleAsync(int id, CancellationToken ct = default) =>
        uow.ServiceRecords.GetByVehicleAsync(id, ct);
    public Task<IReadOnlyList<ServiceRecord>> AllAsync(CancellationToken ct = default) =>
        uow.ServiceRecords.GetAllAsync(ct);
    public Task<ServiceRecord?> GetAsync(int id, CancellationToken ct = default) =>
        uow.ServiceRecords.GetByIdAsync(id, ct);

    public async Task<int> CreateAsync(ServiceRecordInput input, string actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(actorId))
            throw new BusinessRuleException("Zapisující uživatel není přihlášen.");
        var vehicle = await ValidateAsync(input, ct);
        var record = new ServiceRecord { CreatedById = actorId };
        Apply(input, record);
        await uow.ServiceRecords.AddAsync(record, ct);
        if (record.Status == ServiceRecordStatus.Completed)
            vehicle.Mileage = Math.Max(vehicle.Mileage, record.MileageAtService);
        await uow.SaveChangesAsync(ct);
        return record.Id;
    }

    public async Task UpdateAsync(int id, ServiceRecordInput input, CancellationToken ct = default)
    {
        var record = await uow.ServiceRecords.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException("Servisní záznam neexistuje.");
        var vehicle = await ValidateAsync(input, ct);
        Apply(input, record);
        if (record.Status == ServiceRecordStatus.Completed)
            vehicle.Mileage = Math.Max(vehicle.Mileage, record.MileageAtService);
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var record = await uow.ServiceRecords.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException("Servisní záznam neexistuje.");
        uow.ServiceRecords.Remove(record);
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Vehicle> ValidateAsync(ServiceRecordInput input, CancellationToken ct)
    {
        var vehicle = await uow.Vehicles.GetByIdAsync(input.VehicleId, ct)
            ?? throw new BusinessRuleException("Vozidlo neexistuje.");
        if (await uow.ServiceTypes.GetByIdAsync(input.ServiceTypeId, ct) is null)
            throw new BusinessRuleException("Neplatný servisní úkon.");
        if (input.ServiceCenterId.HasValue &&
            await uow.ServiceCenters.GetByIdAsync(input.ServiceCenterId.Value, ct) is null)
            throw new BusinessRuleException("Neplatný servis.");
        if (!Enum.IsDefined(input.Status)) throw new BusinessRuleException("Neplatný stav záznamu.");
        if (input.ServiceDate == default)
            throw new BusinessRuleException("Zadejte datum servisu.");
        if (input.Status == ServiceRecordStatus.Completed && input.ServiceDate.Date > DateTime.Today)
            throw new BusinessRuleException("Dokončený servis nemůže být v budoucnu.");
        if (input.MileageAtService < 0 || input.Price < 0 || input.Price > 99999999m)
            throw new BusinessRuleException("Kilometry a cena musí být nezáporné.");
        if (input.Note?.Length > 1000) throw new BusinessRuleException("Poznámka je příliš dlouhá.");
        return vehicle;
    }

    private static void Apply(ServiceRecordInput input, ServiceRecord record)
    {
        record.VehicleId = input.VehicleId;
        record.ServiceTypeId = input.ServiceTypeId;
        record.ServiceCenterId = input.ServiceCenterId;
        record.ServiceDate = input.ServiceDate;
        record.MileageAtService = input.MileageAtService;
        record.Price = input.Price;
        record.Status = input.Status;
        record.Note = input.Note?.Trim();
    }
}
