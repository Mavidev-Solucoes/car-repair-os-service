using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ServiceOrderRepository : Repository<ServiceOrder>, IServiceOrderRepository
{
    public ServiceOrderRepository(CarRepairOsDbContext context) : base(context)
    {
    }

    public async Task<ServiceOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Context.Set<ServiceOrder>()
            .Include(order => order.ServiceItems)
            .Include(order => order.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<(IEnumerable<ServiceOrder> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceOrder, bool>>>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<ServiceOrder>()
            .AsNoTracking()
            .AsQueryable();

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
            .Include(order => order.ServiceItems)
            .Include(order => order.StatusHistory)
            .AsSplitQuery()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<ServiceOrder> ApplyOrdering(IQueryable<ServiceOrder> query, string? orderBy, bool orderDescending)
        => (orderBy?.Trim().ToLowerInvariant()) switch
        {
            "status" => orderDescending ? query.OrderByDescending(order => order.Status) : query.OrderBy(order => order.Status),
            "totalprice" => orderDescending ? query.OrderByDescending(order => order.TotalPrice) : query.OrderBy(order => order.TotalPrice),
            "createdat" => orderDescending ? query.OrderByDescending(order => order.CreatedAt) : query.OrderBy(order => order.CreatedAt),
            _ => query.OrderByDescending(order => order.CreatedAt)
        };
}
