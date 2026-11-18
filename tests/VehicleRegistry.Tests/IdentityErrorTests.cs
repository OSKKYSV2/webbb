using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Infrastructure;
using VehicleRegistry.Infrastructure.Identity;
using VehicleRegistry.Infrastructure.Persistence;
using Xunit;

namespace VehicleRegistry.Tests;

public sealed class IdentityErrorTests
{
    [Fact]
    public void Describer_ReturnsCzechText_AndKeepsIdentityCodes()
    {
        var describer = new CzechIdentityErrorDescriber();

        var duplicate = describer.DuplicateEmail("jan@test.invalid");
        Assert.Equal(nameof(IdentityErrorDescriber.DuplicateEmail), duplicate.Code);
        Assert.Equal("Účet s e-mailem 'jan@test.invalid' už existuje.", duplicate.Description);

        var tooShort = describer.PasswordTooShort(8);
        Assert.Equal(nameof(IdentityErrorDescriber.PasswordTooShort), tooShort.Code);
        Assert.Equal("Heslo musí mít alespoň 8 znaků.", tooShort.Description);
    }

    [Fact]
    public void AddInfrastructure_RegistersCzechDescriber()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Unused;Trusted_Connection=True"
            })
            .Build();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<CzechIdentityErrorDescriber>(scope.ServiceProvider.GetRequiredService<IdentityErrorDescriber>());
    }

    [Fact]
    public async Task DuplicateAccount_ProducesOneCzechMessage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var describer = new CzechIdentityErrorDescriber();
        using var store = new UserStore<ApplicationUser>(db, describer);
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var manager = new UserManager<ApplicationUser>(
            store,
            // Same user and password rules as DependencyInjection.AddInfrastructure.
            Options.Create(new IdentityOptions
            {
                User = { RequireUniqueEmail = true },
                Password = { RequiredLength = 8, RequireNonAlphanumeric = false }
            }),
            new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>(describer)],
            [new PasswordValidator<ApplicationUser>(describer)],
            new UpperInvariantLookupNormalizer(),
            describer,
            provider,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        static ApplicationUser NewUser() => new()
        {
            UserName = "jan@test.invalid", Email = "jan@test.invalid", FirstName = "Jan", LastName = "Novák"
        };

        var first = await manager.CreateAsync(NewUser(), "Heslo1234");
        Assert.True(first.Succeeded, string.Join("; ", first.Errors.Select(e => e.Description)));
        var duplicate = await manager.CreateAsync(NewUser(), "Heslo1234");

        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
        Assert.Contains(duplicate.Errors, e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail));
        // Controllers show distinct descriptions, so the user sees the message once.
        var shown = Assert.Single(duplicate.Errors.Select(e => e.Description).Distinct());
        Assert.Equal("Účet s e-mailem 'jan@test.invalid' už existuje.", shown);
    }
}
