using Yandex.Application.Dtos;
using Yandex.Application.Requests.Events;

namespace Yandex.Application.Abstractions;

public interface IEventService
{
    IEnumerable<EventDto> GetEvents(EventFilter filter);
    EventDto GetEvent(Guid id);
    EventDto CreateEvent(CreateEventRequest request);
    EventDto UpdateEvent(Guid id, UpdateEventRequest request);
    void DeleteEvent(Guid id);
}