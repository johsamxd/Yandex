using EventService.Application.Dtos.Events;
using EventService.Application.Requests.Events;
using Shared.Application.Dtos;

namespace EventService.Application.Abstractions.Services;

public interface IEventService
{
    Task<PaginatedResult<EventDto>> GetEventsAsync(EventFilter filter, CancellationToken cancellationToken = default);
    Task<EventDto> GetEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDto> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateEventAsync(Guid id, UpdateEventRequest request, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}