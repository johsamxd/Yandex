using Yandex.Domain.Entities;

namespace Yandex.Domain.Abstractions;

public interface IEntityRepository<T> where T : BaseEntity
{
    IQueryable<T> Query();

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<T>> ToListAsync(IQueryable<T> queryable, CancellationToken cancellationToken = default);
}