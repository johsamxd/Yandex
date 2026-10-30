using Microsoft.EntityFrameworkCore;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Infrastructure.Persistence;

public class EntityRepository<T>(AppDbContext context) : IEntityRepository<T>
    where T : BaseEntity
{
    private readonly DbSet<T> _set = context.Set<T>();

    public IQueryable<T> Query() => _set.AsQueryable();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _set.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var existing = await _set.FindAsync([entity.Id], cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Entity with id {entity.Id} not found");
        }

        context.Entry(existing).CurrentValues.SetValues(entity);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _set.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Entity with id {id} not found");
        }

        _set.Remove(existing);
    }

    public Task<List<T>> ToListAsync(
        IQueryable<T> queryable,
        CancellationToken cancellationToken = default)
    {
        return queryable.ToListAsync(cancellationToken);
    }

}