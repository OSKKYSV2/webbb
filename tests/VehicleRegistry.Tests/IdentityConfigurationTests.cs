using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VehicleRegistry.Infrastructure;
using Xunit;

namespace VehicleRegistry.Tests;

public sealed class IdentityConfigurationTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Unused;Trusted_Connection=True"
            })
            .Build();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AccountChanges_AreRevalidatedOnEveryRequest()
    {
        using var provider = BuildProvider();
        var options = provider.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value;
        Assert.Equal(TimeSpan.Zero, options.ValidationInterval);
    }

    [Fact]
    public void FailedLogins_LockTheAccountAfterFiveAttempts()
    {
        using var provider = BuildProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.True(options.User.RequireUniqueEmail);
        Assert.Equal(8, options.Password.RequiredLength);
    }
}
