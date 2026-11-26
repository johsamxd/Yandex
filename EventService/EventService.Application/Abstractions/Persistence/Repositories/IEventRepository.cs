using EventService.Application.Dtos.Events;
using EventService.Domain.Entities;

namespace EventService.Application.Abstractions.Persistence.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetFilteredAsync(EventQuery filter, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(EventQuery filter, CancellationToken cancellationToken = default);

    Task AddAsync(Event entity, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}