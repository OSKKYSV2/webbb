using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleRegistry.Domain.Entities;


namespace VehicleRegistry.Infrastructure.Persistence.Configurations;

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Country).HasMaxLength(100);
        b.HasIndex(x => x.Name).IsUnique();
        b.HasData(
            new Manufacturer { Id = 1, Name = "Škoda", Country = "Česko", CreatedAt = new DateTime(2026, 1, 1) },
            new Manufacturer { Id = 2, Name = "Volkswagen", Country = "Německo", CreatedAt = new DateTime(2026, 1, 1) },
            new Manufacturer { Id = 3, Name = "Toyota", Country = "Japonsko", CreatedAt = new DateTime(2026, 1, 1) });
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.Property(x => x.Vin).IsRequired().HasMaxLength(17).IsFixedLength();
        b.HasIndex(x => x.Vin).IsUnique();
        b.Property(x => x.LicensePlate).IsRequired().HasMaxLength(10);
        b.HasIndex(x => x.LicensePlate).IsUnique();
        b.Property(x => x.Model).IsRequired().HasMaxLength(100);
        b.Property(x => x.Color).HasMaxLength(50);
        b.Property(x => x.ImagePath).HasMaxLength(260);
        b.Property(x => x.FuelType).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Manufacturer)
            .WithMany(m => m.Vehicles)
            .HasForeignKey(x => x.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Owner)
            .WithMany(u => u.Vehicles)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasData(
            new ServiceType { Id = 1, Name = "Výměna oleje", RecommendedIntervalKm = 15000, RecommendedIntervalMonths = 12, CreatedAt = new DateTime(2026, 1, 1) },
            new ServiceType { Id = 2, Name = "STK", RecommendedIntervalMonths = 24, CreatedAt = new DateTime(2026, 1, 1) },
            new ServiceType { Id = 3, Name = "Přezutí pneumatik", RecommendedIntervalMonths = 6, CreatedAt = new DateTime(2026, 1, 1) },
            new ServiceType { Id = 4, Name = "Výměna brzdových destiček", RecommendedIntervalKm = 40000, CreatedAt = new DateTime(2026, 1, 1) });
    }
}

public class ServiceCenterConfiguration : IEntityTypeConfiguration<ServiceCenter>
{
    public void Configure(EntityTypeBuilder<ServiceCenter> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.Address).IsRequired().HasMaxLength(250);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
    }
}

public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRecord> b)
    {
        b.Property(x => x.Price).HasPrecision(10, 2);
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Vehicle)
            .WithMany(v => v.ServiceRecords)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ServiceType)
            .WithMany(t => t.ServiceRecords)
            .HasForeignKey(x => x.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ServiceCenter)
            .WithMany(c => c.ServiceRecords)
            .HasForeignKey(x => x.ServiceCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.CreatedBy)
            .WithMany(u => u.CreatedServiceRecords)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
