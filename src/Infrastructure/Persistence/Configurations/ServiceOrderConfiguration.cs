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

        builder.Property(serviceOrder => serviceOrder.Status)
            .IsRequired();

        builder.Property(serviceOrder => serviceOrder.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(serviceOrder => serviceOrder.CreatedAt).IsRequired();
        builder.Property(serviceOrder => serviceOrder.UpdatedAt);
        builder.Property(serviceOrder => serviceOrder.CreatedUserId);
        builder.Property(serviceOrder => serviceOrder.LastUpdatedUserId);

        builder.HasOne(serviceOrder => serviceOrder.Customer)
            .WithMany()
            .HasForeignKey(serviceOrder => serviceOrder.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(serviceOrder => serviceOrder.Items)
            .WithOne(item => item.ServiceOrder)
            .HasForeignKey(item => item.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(serviceOrder => serviceOrder.StatusHistory)
            .WithOne(history => history.ServiceOrder)
            .HasForeignKey(history => history.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
