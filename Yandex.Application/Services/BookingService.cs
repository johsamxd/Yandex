using AutoMapper;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Bookings;
using Yandex.Application.Exceptions;
using Yandex.Application.Exceptions.Booking;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.Application.Services;

public class BookingService(
    IEntityRepository<Booking> bookingRepository,
    IEntityRepository<Event> eventRepository,
    IMapper mapper) : IBookingService
{
    private readonly object _bookingLock = new();

    public async Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        await Task.Delay(100);
        
        lock (_bookingLock)
        {
            var existingEvent = eventRepository.GetById(eventId);
            if (existingEvent is null)
            {
                throw new NotFoundException($"Event with id: {eventId} not found");
            }

            if (!existingEvent.TryReserveSeats())
            {
                throw new NoAvailableSeatsException("No available seats for this event");
            }

            var data = new Booking
            {
                EventId = eventId,
            };

            bookingRepository.Add(data);
            eventRepository.Update(existingEvent);

            return mapper.Map<BookingDto>(data);
        }
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var data = bookingRepository.GetById(bookingId);
        if (data is null)
        {
            throw new NotFoundException($"Booking with id: {bookingId} not found");
        }

        await Task.Delay(100);

        return mapper.Map<BookingDto>(data);
    }
}