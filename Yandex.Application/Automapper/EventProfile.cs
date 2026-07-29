using AutoMapper;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Requests.Events;
using Yandex.Domain.Entities;

namespace Yandex.Application.Automapper;

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