using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Application.Services;

public interface ICatalogService
{
    Task<IReadOnlyList<Manufacturer>> ManufacturersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceType>> TypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceCenter>> CentersAsync(CancellationToken ct = default);
    Task<Manufacturer?> ManufacturerAsync(int id, CancellationToken ct = default);
    Task<ServiceType?> TypeAsync(int id, CancellationToken ct = default);
    Task<ServiceCenter?> CenterAsync(int id, CancellationToken ct = default);
    Task SaveManufacturerAsync(int? id, string name, string? country, CancellationToken ct = default);
    Task SaveTypeAsync(int? id, string name, string? description, int? km, int? months, CancellationToken ct = default);
    Task SaveCenterAsync(int? id, string name, string address, string? phone, string? email, CancellationToken ct = default);
    Task DeleteManufacturerAsync(int id, CancellationToken ct = default);
    Task DeleteTypeAsync(int id, CancellationToken ct = default);
    Task DeleteCenterAsync(int id, CancellationToken ct = default);
}
