using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using VehicleRegistry.Application;
using VehicleRegistry.Application.Models;
using VehicleRegistry.Application.Services;
using VehicleRegistry.Domain.Entities;
using VehicleRegistry.Domain.Enums;
using VehicleRegistry.Infrastructure.Persistence;
using VehicleRegistry.Infrastructure.Repositories;
using Xunit;

namespace VehicleRegistry.Tests;

public sealed class RegistryServiceTests
{
    // SQLite's in-memory relational engine enforces foreign keys and exercises
    // the same navigation semantics that EF's non-relational InMemory provider misses.
    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public ApplicationDbContext Db { get; }
        public UnitOfWork Unit { get; }

        public TestDatabase()
        {
            connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection).Options;
            Db = new ApplicationDbContext(options);
            Db.Database.EnsureCreated();
            Db.Users.AddRange(
                new ApplicationUser { Id = "owner-a", UserName = "owner-a@test.invalid", Email = "owner-a@test.invalid" },
                new ApplicationUser { Id = "owner-b", UserName = "owner-b@test.invalid", Email = "owner-b@test.invalid" },
                new ApplicationUser { Id = "manager", UserName = "manager@test.invalid", Email = "manager@test.invalid" });
            Db.SaveChanges();
            Unit = new UnitOfWork(Db, new VehicleRepository(Db), new ServiceRecordRepository(Db),
                new ManufacturerRepository(Db), new ServiceTypeRepository(Db), new ServiceCenterRepository(Db));
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static VehicleInput Example(string vin = "TMBJJ7NE6H0123456", string plate = "1AB2345") =>
        new(vin, plate, "Octavia", 2020, FuelType.Petrol, 120000, "Modrá", 1);

    [Fact]
    public async Task Customer_CannotReadAnotherOwnersVehicle()
    {
        await using var fixture = new TestDatabase();
        var db = fixture.Db;
        var unit = fixture.Unit;
        {
            var service = new VehicleService(unit);
            var added = await service.CreateAsync(Example(), "owner-a");
            Assert.Null(await service.GetVisibleAsync(added.Id, "owner-b", false));
            Assert.NotNull(await service.GetVisibleAsync(added.Id, "owner-a", false));
        }
    }

    [Fact]
    public async Task Customer_CannotEditAnotherOwnersVehicle()
    {
        await using var fixture = new TestDatabase();
        var db = fixture.Db;
        var unit = fixture.Unit;
        {
            var service = new VehicleService(unit);
            var vehicle = await service.CreateAsync(Example(), "owner-a");
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                service.UpdateAsync(vehicle.Id, Example(), "owner-b", false));
        }
    }

    [Fact]
    public async Task VehicleService_DoesNotAllowDuplicateVin()
    {
        await using var fixture = new TestDatabase();
        var db = fixture.Db;
        var unit = fixture.Unit;
        {
            var service = new VehicleService(unit);
            await service.CreateAsync(Example(), "owner-a");
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                service.CreateAsync(Example(plate: "2BC3456"), "owner-b"));
        }
    }

    [Fact]
    public async Task CompletedService_IncreasesRecordedMileage()
    {
        await using var fixture = new TestDatabase();
        var db = fixture.Db;
        var unit = fixture.Unit;
        {
            var service = new VehicleService(unit);
            var history = new ServiceHistoryService(unit);
            var vehicle = await service.CreateAsync(Example(), "owner-a");
            await history.CreateAsync(new ServiceRecordInput(vehicle.Id, 1, null,
                DateTime.Today, 130000, 2500, ServiceRecordStatus.Completed, "Vymenil olej"), "manager");
            var changed = await unit.Vehicles.GetByIdAsync(vehicle.Id);
            Assert.Equal(130000, changed?.Mileage);
        }
    }

    [Fact]
    public async Task CompletedService_InFuture_IsRejectedByBusinessLayer()
    {
        await using var fixture = new TestDatabase();
        var db = fixture.Db;
        var unit = fixture.Unit;
        {
            var vehicle = await new VehicleService(unit).CreateAsync(Example(), "owner-a");
            var history = new ServiceHistoryService(unit);
            await Assert.ThrowsAsync<BusinessRuleException>(() =>
                history.CreateAsync(new ServiceRecordInput(vehicle.Id, 1, null,
                    DateTime.Today.AddDays(1), 121000, 100m, ServiceRecordStatus.Completed, null), "manager"));
        }
    }

    [Fact]
    public async Task ServiceRecord_CreatorNavigation_ResolvesToIdentityUser()
    {
        await using var fixture = new TestDatabase();
        var vehicle = await new VehicleService(fixture.Unit).CreateAsync(Example(), "owner-a");
        var history = new ServiceHistoryService(fixture.Unit);
        var recordId = await history.CreateAsync(new ServiceRecordInput(vehicle.Id, 1, null,
            DateTime.Today, 121000, 1000m, ServiceRecordStatus.Completed, null), "manager");

        var record = await fixture.Unit.ServiceRecords.GetByIdAsync(recordId);
        Assert.Equal("manager", record?.CreatedBy?.Id);
    }

    [Fact]
    public async Task Sqlite_EnforcesVehicleOwnerForeignKey()
    {
        await using var fixture = new TestDatabase();
        var service = new VehicleService(fixture.Unit);
        await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(Example(), "missing-user"));
    }

    [Fact]
    public async Task ServiceRecords_RejectCompletionInFuture()
    {
        await using var fixture = new TestDatabase();
        var vehicle = await new VehicleService(fixture.Unit).CreateAsync(Example(), "owner-a");
        var history = new ServiceHistoryService(fixture.Unit);
        await Assert.ThrowsAsync<BusinessRuleException>(() => history.CreateAsync(
            new ServiceRecordInput(vehicle.Id, 1, null, DateTime.Today.AddDays(2), 122000, 100m,
                ServiceRecordStatus.Completed, null), "manager"));
    }

    [Fact]
    public async Task DeletingServiceCreator_PreservesHistoricalRecord()
    {
        await using var fixture = new TestDatabase();
        var service = new VehicleService(fixture.Unit);
        var vehicle = await service.CreateAsync(Example(), "owner-a");
        var history = new ServiceHistoryService(fixture.Unit);
        var recordId = await history.CreateAsync(new ServiceRecordInput(vehicle.Id, 1, null,
            DateTime.Today, 120000, 200m, ServiceRecordStatus.Completed, "Kontrola"), "manager");

        var manager = await fixture.Db.Users.FindAsync("manager");
        Assert.NotNull(manager);
        fixture.Db.Users.Remove(manager!);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var record = await fixture.Unit.ServiceRecords.GetByIdAsync(recordId);
        Assert.NotNull(record);
        Assert.Null(record.CreatedById);
    }

    [Fact]
    public async Task CannotDeleteManufacturerUsedByVehicle()
    {
        await using var fixture = new TestDatabase();
        await new VehicleService(fixture.Unit).CreateAsync(Example(), "owner-a");
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new CatalogService(fixture.Unit).DeleteManufacturerAsync(1));
    }

    [Fact]
    public async Task ReusingLicensePlate_IsRejected()
    {
        await using var fixture = new TestDatabase();
        var service = new VehicleService(fixture.Unit);
        await service.CreateAsync(Example(), "owner-a");
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateAsync(Example(vin: "TMBJJ7NE6H0654321"), "owner-b"));
    }

}
