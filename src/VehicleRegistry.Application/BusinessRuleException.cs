namespace VehicleRegistry.Application;

public sealed class BusinessRuleException(string message) : Exception(message);
