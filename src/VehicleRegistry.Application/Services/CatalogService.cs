using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Domain.Repositories;

namespace VehicleRegistry.Application.Services;

public sealed class CatalogService(IUnitOfWork uow) : ICatalogService
{
    public Task<IReadOnlyList<Manufacturer>> ManufacturersAsync(CancellationToken ct = default) => uow.Manufacturers.GetAllAsync(ct);
    public Task<IReadOnlyList<ServiceType>> TypesAsync(CancellationToken ct = default) => uow.ServiceTypes.GetAllAsync(ct);
    public Task<IReadOnlyList<ServiceCenter>> CentersAsync(CancellationToken ct = default) => uow.ServiceCenters.GetAllAsync(ct);
    public Task<Manufacturer?> ManufacturerAsync(int id, CancellationToken ct = default) => uow.Manufacturers.GetByIdAsync(id, ct);
    public Task<ServiceType?> TypeAsync(int id, CancellationToken ct = default) => uow.ServiceTypes.GetByIdAsync(id, ct);
    public Task<ServiceCenter?> CenterAsync(int id, CancellationToken ct = default) => uow.ServiceCenters.GetByIdAsync(id, ct);

    public async Task SaveManufacturerAsync(int? id, string name, string? country, CancellationToken ct = default)
    {
        Required(name, 100, "Název výrobce");
        Optional(country, 100, "Země");
        var existing = await uow.Manufacturers.GetAllAsync(ct);
        if (existing.Any(m => m.Id != id && string.Equals(m.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException("Výrobce s tímto názvem již existuje.");
        Manufacturer manufacturer;
        if (id.HasValue)
            manufacturer = await uow.Manufacturers.GetByIdAsync(id.Value, ct)
                ?? throw new BusinessRuleException("Výrobce neexistuje.");
        else
        {
            manufacturer = new Manufacturer();
            await uow.Manufacturers.AddAsync(manufacturer, ct);
        }
        manufacturer.Name = name.Trim();
        manufacturer.Country = country?.Trim();
        await uow.SaveChangesAsync(ct);
    }

    public async Task SaveTypeAsync(int? id, string name, string? description, int? km, int? months, CancellationToken ct = default)
    {
        Required(name, 100, "Název úkonu");
        Optional(description, 500, "Popis");
        if (km is <= 0 || months is <= 0)
            throw new BusinessRuleException("Servisní interval musí být kladné číslo.");
        ServiceType serviceType;
        if (id.HasValue)
            serviceType = await uow.ServiceTypes.GetByIdAsync(id.Value, ct)
                ?? throw new BusinessRuleException("Druh úkonu neexistuje.");
        else
        {
            serviceType = new ServiceType();
            await uow.ServiceTypes.AddAsync(serviceType, ct);
        }
        serviceType.Name = name.Trim();
        serviceType.Description = description?.Trim();
        serviceType.RecommendedIntervalKm = km;
        serviceType.RecommendedIntervalMonths = months;
        await uow.SaveChangesAsync(ct);
    }

    public async Task SaveCenterAsync(int? id, string name, string address, string? phone, string? email, CancellationToken ct = default)
    {
        Required(name, 150, "Název servisu");
        Required(address, 250, "Adresa");
        Optional(phone, 20, "Telefon");
        Optional(email, 150, "E-mail");
        ServiceCenter serviceCenter;
        if (id.HasValue)
            serviceCenter = await uow.ServiceCenters.GetByIdAsync(id.Value, ct)
                ?? throw new BusinessRuleException("Servis neexistuje.");
        else
        {
            serviceCenter = new ServiceCenter();
            await uow.ServiceCenters.AddAsync(serviceCenter, ct);
        }
        serviceCenter.Name = name.Trim();
        serviceCenter.Address = address.Trim();
        serviceCenter.Phone = phone?.Trim();
        serviceCenter.Email = email?.Trim();
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteManufacturerAsync(int id, CancellationToken ct = default)
    {
        var m = await uow.Manufacturers.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException("Výrobce neexistuje.");
        if (await uow.Manufacturers.IsUsedAsync(id, ct))
            throw new BusinessRuleException("Výrobce je použit u vozidla, nelze jej smazat.");
        uow.Manufacturers.Remove(m);
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteTypeAsync(int id, CancellationToken ct = default)
    {
        var t = await uow.ServiceTypes.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException("Druh úkonu neexistuje.");
        if (await uow.ServiceTypes.IsUsedAsync(id, ct))
            throw new BusinessRuleException("Druh úkonu má servisní historii, nelze jej smazat.");
        uow.ServiceTypes.Remove(t);
        await uow.SaveChangesAsync(ct);
    }

    public async Task DeleteCenterAsync(int id, CancellationToken ct = default)
    {
        var c = await uow.ServiceCenters.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException("Servis neexistuje.");
        if (await uow.ServiceCenters.IsUsedAsync(id, ct))
            throw new BusinessRuleException("Servis má záznamy, nejprve je upravte.");
        uow.ServiceCenters.Remove(c);
        await uow.SaveChangesAsync(ct);
    }

    private static void Required(string? value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new BusinessRuleException($"{label} je povinný (maximálně {max} znaků).");
    }
    private static void Optional(string? value, int max, string label)
    {
        if (value?.Trim().Length > max)
            throw new BusinessRuleException($"{label} může mít maximálně {max} znaků.");
    }
}
