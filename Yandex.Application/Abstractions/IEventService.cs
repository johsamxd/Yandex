using Yandex.Application.Dtos;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Requests.Events;

namespace Yandex.Application.Abstractions;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetEventsAsync(EventFilter filter, CancellationToken cancellationToken = default);
    Task<EventDto> GetEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDto> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}