using Application.Common.Interfaces;
using Domain.Interfaces.Repositories;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        services.AddDbContext<CarRepairOsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CarRepairOsDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();

        return services;
    }
}
