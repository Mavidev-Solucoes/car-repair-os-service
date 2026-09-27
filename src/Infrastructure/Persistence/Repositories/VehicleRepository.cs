using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class VehicleRepository : Repository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(CarRepairOsDbContext context) : base(context)
    {
    }

    public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLicensePlate(licensePlate);
        return await Context.Set<Vehicle>().AsNoTracking().FirstOrDefaultAsync(vehicle => vehicle.LicensePlate == normalized, cancellationToken);
    }

    public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => await Context.Set<Vehicle>().AsNoTracking().Where(vehicle => vehicle.CustomerId == customerId).ToListAsync(cancellationToken);

    public async Task<bool> ExistsByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLicensePlate(licensePlate);
        return await Context.Set<Vehicle>().AnyAsync(vehicle => vehicle.LicensePlate == normalized, cancellationToken);
    }

    public Task<bool> HasServiceOrdersAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Context.ServiceOrders.AnyAsync(serviceOrder => serviceOrder.VehicleId == vehicleId, cancellationToken);

    public async Task<(IEnumerable<Vehicle> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? orderBy, bool orderDescending, IEnumerable<Expression<Func<Vehicle, bool>>>? filters = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Vehicle>().AsNoTracking().AsQueryable();

        if (filters is not null)
        {
            foreach (var filter in filters)
            {
                query = query.Where(filter);
            }
        }

        query = ApplyOrdering(query, orderBy, orderDescending);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Vehicle> ApplyOrdering(IQueryable<Vehicle> query, string? orderBy, bool orderDescending)
        => (orderBy?.Trim().ToLowerInvariant()) switch
        {
            "brand" => orderDescending ? query.OrderByDescending(vehicle => vehicle.Brand) : query.OrderBy(vehicle => vehicle.Brand),
            "model" => orderDescending ? query.OrderByDescending(vehicle => vehicle.Model) : query.OrderBy(vehicle => vehicle.Model),
            "year" => orderDescending ? query.OrderByDescending(vehicle => vehicle.Year) : query.OrderBy(vehicle => vehicle.Year),
            "licenseplate" => orderDescending ? query.OrderByDescending(vehicle => vehicle.LicensePlate) : query.OrderBy(vehicle => vehicle.LicensePlate),
            "createdat" => orderDescending ? query.OrderByDescending(vehicle => vehicle.CreatedAt) : query.OrderBy(vehicle => vehicle.CreatedAt),
            _ => query.OrderBy(vehicle => vehicle.Brand)
        };

    private static string NormalizeLicensePlate(string value) =>
        new string(value.Where(c => c != '-').ToArray()).ToUpperInvariant();
}
