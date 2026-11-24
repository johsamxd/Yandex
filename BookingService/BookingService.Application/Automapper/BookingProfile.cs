using AutoMapper;
using BookingService.Application.Dtos.Bookings;
using BookingService.Domain.Entities;

namespace BookingService.Application.Automapper;

public class BookingProfile : Profile
{
    public BookingProfile()
    {
        CreateMap<Booking, BookingDto>().ReverseMap();
    }
}