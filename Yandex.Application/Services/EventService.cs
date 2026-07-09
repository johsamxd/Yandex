using AutoMapper;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Events;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Application.Services;

public class EventService(IEntityRepository<Event> repository, IMapper mapper) : IEventService
{
    public IEnumerable<EventDto> GetEvents()
    {
        var data = repository.GetAll();

        return mapper.Map<IEnumerable<EventDto>>(data);
    }

    public EventDto GetEvent(Guid id)
    {
        var data = repository.GetById(id);

        return mapper.Map<EventDto>(data);
    }

    public void CreateEvent(CreateEventRequest request)
    {
        var data = mapper.Map<Event>(request);

        repository.Add(data);
    }

    public void UpdateEvent(Guid id, UpdateEventRequest request)
    {
        var data = mapper.Map<Event>(request);

        if (data == null)
            throw new KeyNotFoundException($"Entity with id {id} not found");

        repository.Update(data);
    }

    public void DeleteEvent(Guid id)
    {
        repository.Remove(id);
    }
}