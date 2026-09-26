using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceOrderItemConfiguration : IEntityTypeConfiguration<ServiceOrderItem>
{
    public void Configure(EntityTypeBuilder<ServiceOrderItem> builder)
    {
        builder.ToTable("service_order_items");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.Ignore(item => item.Total);

        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt);
        builder.Property(item => item.CreatedUserId);
        builder.Property(item => item.LastUpdatedUserId);
    }
}
