using System.Linq.Expressions;
using System.Data;
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

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Context.Customers.AnyAsync(customer => customer.Email == normalized, cancellationToken);
    }

    public Task<bool> HasVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        LegacyTableHasCustomerAsync("public.vehicles", "SELECT EXISTS (SELECT 1 FROM public.vehicles WHERE customer_id = @customerId)", customerId, cancellationToken);

    public Task<bool> HasServiceOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        LegacyTableHasCustomerAsync("public.service_orders", "SELECT EXISTS (SELECT 1 FROM public.service_orders WHERE customer_id = @customerId)", customerId, cancellationToken);

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? orderBy, bool orderDescending, IEnumerable<Expression<Func<Customer, bool>>>? filters = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Customers.AsNoTracking().AsQueryable();

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

    private async Task<bool> LegacyTableHasCustomerAsync(string tableName, string existsSql, Guid customerId, CancellationToken cancellationToken)
    {
        if (!Context.Database.IsNpgsql())
        {
            return false;
        }

        var connection = Context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var tableCommand = connection.CreateCommand();
            tableCommand.CommandText = "SELECT to_regclass(@tableName)";
            var tableParameter = tableCommand.CreateParameter();
            tableParameter.ParameterName = "tableName";
            tableParameter.Value = tableName;
            tableCommand.Parameters.Add(tableParameter);

            var tableExists = await tableCommand.ExecuteScalarAsync(cancellationToken);
            if (tableExists is null or DBNull)
            {
                return false;
            }

            await using var relationCommand = connection.CreateCommand();
            relationCommand.CommandText = existsSql;
            var customerParameter = relationCommand.CreateParameter();
            customerParameter.ParameterName = "customerId";
            customerParameter.Value = customerId;
            relationCommand.Parameters.Add(customerParameter);

            var result = await relationCommand.ExecuteScalarAsync(cancellationToken);
            return result is not null and not DBNull && Convert.ToBoolean(result);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
