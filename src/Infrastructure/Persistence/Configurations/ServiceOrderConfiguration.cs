using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("service_orders");
        builder.HasKey(serviceOrder => serviceOrder.Id);

        builder.Property(serviceOrder => serviceOrder.VehicleId)
            .IsRequired();

        builder.Property(serviceOrder => serviceOrder.CustomerId)
            .IsRequired();

        builder.Property(serviceOrder => serviceOrder.AssignedUserId)
            .IsRequired();

        builder.Property(serviceOrder => serviceOrder.Status)
            .IsRequired();

        builder.Property(serviceOrder => serviceOrder.TotalPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(serviceOrder => serviceOrder.CreatedAt).IsRequired();
        builder.Property(serviceOrder => serviceOrder.UpdatedAt);
        builder.Property(serviceOrder => serviceOrder.CreatedUserId);
        builder.Property(serviceOrder => serviceOrder.LastUpdatedUserId);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(serviceOrder => serviceOrder.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(serviceOrder => serviceOrder.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(serviceOrder => serviceOrder.ServiceItems)
            .WithOne()
            .HasForeignKey(item => item.ServiceOrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(serviceOrder => serviceOrder.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.ServiceOrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);



        builder.Metadata.FindNavigation(nameof(ServiceOrder.ServiceItems))!
            .SetField("_serviceItems");
        builder.Metadata.FindNavigation(nameof(ServiceOrder.ServiceItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata.FindNavigation(nameof(ServiceOrder.StatusHistory))!
            .SetField("_statusHistory");
        builder.Metadata.FindNavigation(nameof(ServiceOrder.StatusHistory))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(serviceOrder => serviceOrder.CustomerId);
        builder.HasIndex(serviceOrder => serviceOrder.VehicleId);
        builder.HasIndex(serviceOrder => serviceOrder.Status);
        builder.HasIndex(serviceOrder => serviceOrder.CreatedAt);
    }
}
