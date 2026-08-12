using AutoMapper;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Exceptions;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Application.Services;

public class BookingService(
    IEntityRepository<Booking> bookingRepository,
    IEntityRepository<Event> eventRepository,
    IMapper mapper) : IBookingService
{
    public BookingDto CreateBookingAsync(Guid eventId)
    {
        var existingEvent = eventRepository.GetById(eventId);
        if (existingEvent is null) throw new NotFoundException($"Event with id: {eventId} not found");

        var data = new Booking
        {
            EventId = eventId,
        };

        bookingRepository.Add(data);

        return mapper.Map<BookingDto>(data);
    }

    public BookingDto GetBookingByIdAsync(Guid bookingId)
    {
        var data = bookingRepository.GetById(bookingId);
        if (data is null) throw new NotFoundException($"Booking with id: {bookingId} not found");
        
        return mapper.Map<BookingDto>(data);
    }
}