using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceStatusHistoryConfiguration : IEntityTypeConfiguration<ServiceStatusHistory>
{
    public void Configure(EntityTypeBuilder<ServiceStatusHistory> builder)
    {
        builder.ToTable("service_status_history");
        builder.HasKey(history => history.Id);

        builder.Property(history => history.FromStatus);
        builder.Property(history => history.ToStatus).IsRequired();
        builder.Property(history => history.ChangedAt).IsRequired();
        builder.Property(history => history.ChangedByUserId);
        builder.Property(history => history.CreatedAt).IsRequired();
        builder.Property(history => history.UpdatedAt);
        builder.Property(history => history.CreatedUserId);
        builder.Property(history => history.LastUpdatedUserId);

        builder.HasIndex(history => history.ServiceOrderId);
    }
}
