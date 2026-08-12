using AutoMapper;
using Yandex.Application.Dtos.Bookings;
using Yandex.Domain.Entities;

namespace Yandex.Application.Automapper;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking, BookingDto>().ReverseMap();
    }
}