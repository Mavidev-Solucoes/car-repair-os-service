using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");
        builder.HasKey(vehicle => vehicle.Id);

        builder.Property(vehicle => vehicle.CustomerId)
            .IsRequired();

        builder.Property(vehicle => vehicle.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vehicle => vehicle.Model)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(vehicle => vehicle.Year)
            .IsRequired();

        builder.Property(vehicle => vehicle.LicensePlate)
            .IsRequired()
            .HasMaxLength(7);

        builder.HasIndex(vehicle => vehicle.LicensePlate)
            .IsUnique();

        builder.Property(vehicle => vehicle.Color)
            .HasMaxLength(50);

        builder.Property(vehicle => vehicle.CreatedAt).IsRequired();
        builder.Property(vehicle => vehicle.UpdatedAt);
        builder.Property(vehicle => vehicle.CreatedUserId);
        builder.Property(vehicle => vehicle.LastUpdatedUserId);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(vehicle => vehicle.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
