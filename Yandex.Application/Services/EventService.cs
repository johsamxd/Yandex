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

    public EventDto CreateEvent(CreateEventRequest request)
    {
        var data = mapper.Map<Event>(request);

        repository.Add(data);
        
        return mapper.Map<EventDto>(data);
    }

    public EventDto UpdateEvent(Guid id, UpdateEventRequest request)
    {
        var data = repository.GetById(id);
        
        if (data == null)
            throw new NotFoundException($"Event with id {id} not found");
        
        mapper.Map(request, data);
        repository.Update(data);
        
        return mapper.Map<EventDto>(data);
    }

    public void DeleteEvent(Guid id)
    {
        var data = repository.GetById(id);
        
        if (data == null)
            throw new NotFoundException($"Event with id {id} not found");
        
        repository.Remove(id);
    }
}