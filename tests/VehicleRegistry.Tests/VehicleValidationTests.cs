using System.ComponentModel.DataAnnotations;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Web.Models.ServiceRecords;
using VehicleRegistry.Web.Validation;
using Xunit;

namespace VehicleRegistry.Tests;

public sealed class VehicleValidationTests
{
    [Theory]
    [InlineData("TMBJJ7NE6H0123456", true)]
    [InlineData("tmbjj7ne6h0123456", true)]
    [InlineData("TMBJJ7NE6H012345", false)]
    [InlineData("TMBIJ7NE6H0123456", false)]
    [InlineData("TMBQJ7NE6H0123456", false)]
    [InlineData("", false)]
    public void VinValidation_AcceptsOnlyValidCharactersAndLength(string vin, bool expected)
    {
        Assert.Equal(expected, new VinAttribute().IsValid(vin));
    }

    [Fact]
    public void CompletedService_InFuture_IsRejected()
    {
        var model = new ServiceRecordForm { ServiceDate = DateTime.Today.AddDays(2), Status = ServiceRecordStatus.Completed };
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        Assert.False(valid);
        Assert.Contains(results, r => r.ErrorMessage?.Contains("budoucnu") == true);
    }

    [Fact]
    public void PlannedService_InFuture_IsAllowedByDateValidator()
    {
        var model = new ServiceRecordForm { ServiceDate = DateTime.Today.AddDays(2), Status = ServiceRecordStatus.Planned };
        var attribute = new NotInFutureAttribute();
        var context = new ValidationContext(model) { MemberName = nameof(model.ServiceDate) };
        Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(model.ServiceDate, context));
    }
}
