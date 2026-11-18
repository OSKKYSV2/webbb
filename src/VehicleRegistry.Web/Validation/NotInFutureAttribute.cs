using System.ComponentModel.DataAnnotations;
using VehicleRegistry.Domain.Enums;

namespace VehicleRegistry.Web.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    public NotInFutureAttribute() => ErrorMessage = "Datum dokončeného servisu nesmí být v budoucnu.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime date || validationContext.ObjectInstance is not VehicleRegistry.Web.Models.ServiceRecords.ServiceRecordForm model)
            return ValidationResult.Success;
        return model.Status == ServiceRecordStatus.Completed && date.Date > DateTime.Today
            ? new ValidationResult(ErrorMessage)
            : ValidationResult.Success;
    }
}
