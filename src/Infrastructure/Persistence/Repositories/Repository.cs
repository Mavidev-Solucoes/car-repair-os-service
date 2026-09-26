using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CarRepairOsDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(CarRepairOsDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Delete(T entity) => DbSet.Remove(entity);
}
