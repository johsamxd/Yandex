using AutoMapper;
using Yandex.Application.Abstractions.Persistence;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos.Bookings;
using Yandex.Domain.Entities;
using Yandex.Domain.Exceptions;
using Yandex.Domain.Exceptions.Booking;

namespace Yandex.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
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
                UserId = userId
            };

            await bookingRepository.AddAsync(data, cancellationToken);
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