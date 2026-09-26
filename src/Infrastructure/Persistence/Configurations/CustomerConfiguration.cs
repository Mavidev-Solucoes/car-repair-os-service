using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(customer => customer.PersonalId)
            .IsRequired()
            .HasMaxLength(14)
            .HasComment("Normalized CPF/CNPJ digits only.");

        builder.Property(customer => customer.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(customer => customer.Telephone)
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.Property(customer => customer.CreatedAt).IsRequired();
        builder.Property(customer => customer.UpdatedAt);
        builder.Property(customer => customer.CreatedUserId);
        builder.Property(customer => customer.LastUpdatedUserId);

        builder.HasIndex(customer => customer.PersonalId).IsUnique();
        builder.HasIndex(customer => customer.Email).IsUnique();

        builder.HasMany(customer => customer.Vehicles)
            .WithOne(vehicle => vehicle.Customer)
            .HasForeignKey(vehicle => vehicle.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
