using AutoMapper;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos;
using Yandex.Application.Exceptions;
using Yandex.Application.Requests.Events;
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

        if (data == null) throw new NotFoundException("Event not found"); 

        return mapper.Map<EventDto>(data);
    }

    public void CreateEvent(CreateEventRequest request)
    {
        var data = mapper.Map<Event>(request);

        repository.Add(data);
    }

    public void UpdateEvent(Guid id, UpdateEventRequest request)
    {
        var existing = repository.GetById(id);
        
        if (existing == null)
            throw new NotFoundException($"Event with id {id} not found");
        
        var data = mapper.Map<Event>(request);

        repository.Update(data);
    }

    public void DeleteEvent(Guid id)
    {
        var existing = repository.GetById(id);
        
        if (existing == null)
            throw new NotFoundException($"Event with id {id} not found");
        
        repository.Remove(id);
    }
}