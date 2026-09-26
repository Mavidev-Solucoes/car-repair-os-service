using System.Linq.Expressions;
using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPersonalIdAsync(string personalId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasVehiclesAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> HasServiceOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? orderBy,
        bool orderDescending,
        IEnumerable<Expression<Func<Customer, bool>>>? filters = null,
        CancellationToken cancellationToken = default);
}
