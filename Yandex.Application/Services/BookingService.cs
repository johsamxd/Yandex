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
    IUnitOfWork unitOfWork,
    IMapper mapper) : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await BookingSemaphore.WaitAsync(cancellationToken);

        try
        {
            var existingEvent = await eventRepository.GetByIdAsync(eventId, cancellationToken);
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

            await bookingRepository.AddAsync(data, cancellationToken);
            await eventRepository.UpdateAsync(existingEvent, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<BookingDto>(data);
        }
        finally
        {
            BookingSemaphore.Release();
        }
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var data = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (data is null)
        {
            throw new NotFoundException($"Booking with id: {bookingId} not found");
        }

        return mapper.Map<BookingDto>(data);
    }
}