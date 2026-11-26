using AutoMapper;
using EventService.Application.Dtos.Events;
using EventService.Application.Requests.Events;
using EventService.Domain.Entities;

namespace EventService.Application.Automapper;

public class EventProfile : Profile
{
    public EventProfile()
    {
        CreateMap<Event, EventDto>().ReverseMap();
        CreateMap<CreateEventRequest, Event>();
        CreateMap<UpdateEventRequest, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}