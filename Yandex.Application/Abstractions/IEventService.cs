using Yandex.Application.Dtos;
using Yandex.Application.Requests.Events;

namespace Yandex.Application.Abstractions;

public interface IEventService
{
    IEnumerable<EventDto> GetEvents();
    EventDto GetEvent(Guid id);
    void CreateEvent(CreateEventRequest request);
    void UpdateEvent(Guid id, UpdateEventRequest request);
    void DeleteEvent(Guid id);
}