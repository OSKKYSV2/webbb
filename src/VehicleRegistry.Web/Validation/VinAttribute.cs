using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace VehicleRegistry.Web.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class VinAttribute : ValidationAttribute, IClientModelValidator
{
    private const string Pattern = "^[A-HJ-NPR-Z0-9]{17}$";

    public VinAttribute() => ErrorMessage = "VIN musí obsahovat přesně 17 znaků bez I, O a Q.";

    public override bool IsValid(object? value) =>
        value is string vin && Regex.IsMatch(vin.Trim().ToUpperInvariant(), Pattern, RegexOptions.CultureInvariant);

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-vin", FormatErrorMessage(context.ModelMetadata.DisplayName ?? context.ModelMetadata.Name ?? "VIN"));
    }
}
