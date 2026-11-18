using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Infrastructure.Persistence;
using VehicleRegistry.Domain.Constants;
using VehicleRegistry.Domain.Entities;

namespace VehicleRegistry.Infrastructure.Identity;

public sealed class IdentitySeedHostedService(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<IdentitySeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Run after EF migrations have been applied; no hard-coded credentials are ever committed.
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            throw new InvalidOperationException("Databáze není aktuální. Spusťte dotnet ef database update před startem aplikace.");
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var name in Roles.All)
        {
            if (!await roles.RoleExistsAsync(name))
            {
                var result = await roles.CreateAsync(new IdentityRole(name));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Nepodařilo se vytvořit roli {name}.");
            }
        }

        var adminEmail = config["BootstrapAdmin:Email"];
        var adminPassword = config["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("Bootstrap administrator skipped: no credentials configured");
            return;
        }
        var admin = await users.FindByEmailAsync(adminEmail);
        if (admin is not null) return; // Never reset passwords of existing users.
        admin = new ApplicationUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            FirstName = "Správce",
            LastName = "Systému",
            EmailConfirmed = true
        };
        var created = await users.CreateAsync(admin, adminPassword);
        if (!created.Succeeded)
            throw new InvalidOperationException("Bootstrap admin rejected: " +
                string.Join("; ", created.Errors.Select(e => e.Code)));
        var assigned = await users.AddToRoleAsync(admin, Roles.Admin);
        if (!assigned.Succeeded)
            throw new InvalidOperationException("Bootstrap admin role assignment failed");
        logger.LogInformation("Initial administrator created: {AdminId}", admin.Id);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
