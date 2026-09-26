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

    public async Task<ServiceOrder?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BuildReadDetailedQuery().FirstOrDefaultAsync(serviceOrder => serviceOrder.Id == id, cancellationToken);

    public async Task<ServiceOrder?> GetTrackedWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BuildTrackedDetailedQuery().FirstOrDefaultAsync(serviceOrder => serviceOrder.Id == id, cancellationToken);

    public async Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await BuildReadDetailedQuery().ToListAsync(cancellationToken);

    public async Task<IEnumerable<ServiceStatusHistory>> GetStatusHistoryAsync(Guid serviceOrderId, CancellationToken cancellationToken = default) =>
        await Context.ServiceStatusHistories
            .AsNoTracking()
            .Where(history => history.ServiceOrderId == serviceOrderId)
            .OrderBy(history => history.ChangedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IEnumerable<ServiceOrder> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? orderBy, bool orderDescending, IEnumerable<Expression<Func<ServiceOrder, bool>>>? filters = null, CancellationToken cancellationToken = default)
    {
        var query = BuildReadDetailedQuery();

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

    private IQueryable<ServiceOrder> BuildTrackedDetailedQuery() =>
        Context.ServiceOrders
            .Include(serviceOrder => serviceOrder.Items)
            .Include(serviceOrder => serviceOrder.StatusHistory)
            .Include(serviceOrder => serviceOrder.Customer)
            .Include(serviceOrder => serviceOrder.Vehicle)
            .AsSplitQuery();

    private IQueryable<ServiceOrder> BuildReadDetailedQuery() => BuildTrackedDetailedQuery().AsNoTracking();

    private static IQueryable<ServiceOrder> ApplyOrdering(IQueryable<ServiceOrder> query, string? orderBy, bool orderDescending)
        => (orderBy?.Trim().ToLowerInvariant()) switch
        {
            "status" => orderDescending ? query.OrderByDescending(serviceOrder => serviceOrder.Status) : query.OrderBy(serviceOrder => serviceOrder.Status),
            "totalamount" => orderDescending ? query.OrderByDescending(serviceOrder => serviceOrder.TotalAmount) : query.OrderBy(serviceOrder => serviceOrder.TotalAmount),
            "createdat" => orderDescending ? query.OrderByDescending(serviceOrder => serviceOrder.CreatedAt) : query.OrderBy(serviceOrder => serviceOrder.CreatedAt),
            _ => query.OrderByDescending(serviceOrder => serviceOrder.CreatedAt)
        };
}
