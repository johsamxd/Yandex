using Yandex.Domain.Entities;

namespace Yandex.Domain.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetFilteredAsync(EventQuery filter, int page, int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(EventQuery filter, CancellationToken cancellationToken = default);

    Task AddAsync(Event entity, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}