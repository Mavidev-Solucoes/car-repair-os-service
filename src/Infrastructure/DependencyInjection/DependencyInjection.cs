using Application.Common.Interfaces;
using Application.Common.Messaging;
using Domain.Interfaces.Repositories;
using Infrastructure.Configuration;
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
        var useFallbackDatabaseConfiguration = string.IsNullOrWhiteSpace(connectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
            connectionString = databaseOptions.BuildConnectionString();
        }

        var databaseOptionsBuilder = services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName));

        if (useFallbackDatabaseConfiguration)
        {
            databaseOptionsBuilder
                .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Database host must be provided.")
                .Validate(options => options.Port > 0, "Database port must be greater than zero.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Database), "Database name must be provided.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "Database username must be provided.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "Database password must be provided.")
                .ValidateOnStart();
        }

        var rabbitMqOptionsBuilder = services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName));

        if (configuration.GetValue("RabbitMq:Enabled", true))
        {
            rabbitMqOptionsBuilder
                .Validate(options => !string.IsNullOrWhiteSpace(options.HostName), "RabbitMQ host must be provided.")
                .Validate(options => options.Port > 0, "RabbitMQ port must be greater than zero.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.UserName), "RabbitMQ username must be provided.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMQ password must be provided.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.ExchangeName), "RabbitMQ exchange name must be provided.")
                .Validate(options => options.MaxRetries >= 0, "RabbitMQ max retries cannot be negative.")
                .Validate(options => options.RetryDelayMilliseconds >= 0, "RabbitMQ retry delay cannot be negative.")
                .ValidateOnStart();
        }

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddSingleton<ICommandConsumer, RabbitMqCommandConsumer>();

        services.AddDbContext<CarRepairOsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(CarRepairOsDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                }));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CarRepairOsDbContext>());
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();

        return services;
    }
}
