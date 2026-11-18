namespace VehicleRegistry.Web.Models.Catalogs;

public sealed record CatalogRow(int Id, string Name, string Detail);
public sealed record CatalogList(string Kind, string Title, IReadOnlyList<CatalogRow> Items);
