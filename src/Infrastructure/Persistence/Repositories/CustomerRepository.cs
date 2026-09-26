using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(CarRepairOsDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default)
    {
        var normalized = PersonalId.Normalize(personalId);
        return await Context.Customers.AsNoTracking().FirstOrDefaultAsync(customer => customer.PersonalId == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default)
    {
        var normalized = PersonalId.Normalize(personalId);
        return await Context.Customers.AnyAsync(customer => customer.PersonalId == normalized, cancellationToken);
    }

    public async Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Customers
            .AsNoTracking()
            .Include(customer => customer.Vehicles)
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public async Task<bool> HasVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await Context.Vehicles.AnyAsync(vehicle => vehicle.CustomerId == customerId, cancellationToken);

    public async Task<bool> HasServiceOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await Context.ServiceOrders.AnyAsync(serviceOrder => serviceOrder.CustomerId == customerId, cancellationToken);

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? orderBy, bool orderDescending, IEnumerable<Expression<Func<Customer, bool>>>? filters = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Customers.AsNoTracking().Include(customer => customer.Vehicles).AsQueryable();

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

    private static IQueryable<Customer> ApplyOrdering(IQueryable<Customer> query, string? orderBy, bool orderDescending)
        => (orderBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => orderDescending ? query.OrderByDescending(customer => customer.Name) : query.OrderBy(customer => customer.Name),
            "email" => orderDescending ? query.OrderByDescending(customer => customer.Email) : query.OrderBy(customer => customer.Email),
            "createdat" => orderDescending ? query.OrderByDescending(customer => customer.CreatedAt) : query.OrderBy(customer => customer.CreatedAt),
            _ => query.OrderBy(customer => customer.Name)
        };
}
