namespace VehicleRegistry.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Customer = "Customer";
    public static readonly string[] All = [Admin, Manager, Customer];
}
