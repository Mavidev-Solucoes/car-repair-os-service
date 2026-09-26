using System.Linq.Expressions;
using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task<ServiceOrder?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceOrder>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceStatusHistory>> GetStatusHistoryAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<ServiceOrder> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<ServiceOrder, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
