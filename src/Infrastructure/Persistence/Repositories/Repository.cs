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

    /// <summary>
    /// Entidade carregada por este contexto já é rastreada e o EF detecta as mudanças sozinho; chamar
    /// DbSet.Update nela marcaria filhos novos (ex.: histórico de status) como modificados e o UPDATE
    /// falharia com DbUpdateConcurrencyException. Só anexamos entidades desanexadas.
    /// </summary>
    public virtual void Update(T entity)
    {
        if (Context.Entry(entity).State == EntityState.Detached)
        {
            DbSet.Update(entity);
        }
    }

    public virtual void Delete(T entity) => DbSet.Remove(entity);
}
