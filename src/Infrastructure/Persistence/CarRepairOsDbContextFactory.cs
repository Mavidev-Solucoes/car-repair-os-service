using Application.Common.Interfaces;
using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public sealed class CarRepairOsDbContextFactory : IDesignTimeDbContextFactory<CarRepairOsDbContext>
{
    public CarRepairOsDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../Api"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
            connectionString = databaseOptions.BuildConnectionString();
        }

        var optionsBuilder = new DbContextOptionsBuilder<CarRepairOsDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(CarRepairOsDbContext).Assembly.FullName));

        return new CarRepairOsDbContext(optionsBuilder.Options, NullDomainEventDispatcher.Instance);
    }

    private sealed class NullDomainEventDispatcher : IDomainEventDispatcher
    {
        public static readonly NullDomainEventDispatcher Instance = new();

        public Task DispatchAsync(IEnumerable<Domain.Common.IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
