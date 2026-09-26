using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class CarRepairOsDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public CarRepairOsDbContext(DbContextOptions<CarRepairOsDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderItem> ServiceOrderItems => Set<ServiceOrderItem>();
    public DbSet<ServiceStatusHistory> ServiceStatusHistory => Set<ServiceStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceOrderConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceOrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceStatusHistoryConfiguration());
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(entry => entry.Entity.DomainEvents.Count != 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(entity => entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        domainEntities.ForEach(entity => entity.ClearDomainEvents());
        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }
}
