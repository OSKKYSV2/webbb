using System.ComponentModel.DataAnnotations;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Web.Models.Vehicles;
using Xunit;

namespace VehicleRegistry.Tests;

public sealed class VehicleFormTests
{
    [Fact]
    public void VehicleModel_DoesNotConflictWithMvcModelPrefix()
    {
        Assert.Null(typeof(VehicleForm).GetProperty("Model"));
        var form = new VehicleForm
        {
            Vin = "TMBJJ7NE6H0123456", LicensePlate = "1AB2345",
            VehicleModel = "Octavia", ManufacturerId = 1,
            YearOfManufacture = 2021, FuelType = FuelType.Petrol, Mileage = 55000
        };
        var results = new List<ValidationResult>();
        Assert.True(Validator.TryValidateObject(form, new ValidationContext(form), results, true),
            string.Join("; ", results.Select(x => x.ErrorMessage)));
        Assert.Equal("Octavia", form.ToInput().Model);
    }

    [Fact]
    public void InvalidVin_IsRejected()
    {
        var form = new VehicleForm { Vin = "INVALID", VehicleModel = "Octavia" };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), results, true));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleForm.Vin)));
    }
}
