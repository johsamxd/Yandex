using AutoMapper;
using Yandex.Application.Abstractions.Persistence;
using Yandex.Application.Abstractions.Persistence.Repositories;
using Yandex.Application.Abstractions.Services;
using Yandex.Application.Dtos.Bookings;
using Yandex.Domain.Entities;
using Yandex.Domain.Enums;
using Yandex.Domain.Exceptions;
using Yandex.Domain.Exceptions.Booking;
using Yandex.Domain.Exceptions.Event;

namespace Yandex.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IBookingService
{
    private static readonly SemaphoreSlim BookingSemaphore = new(1, 1);
    private const int MaxActiveBookingsPerUser = 10;

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        await BookingSemaphore.WaitAsync(cancellationToken);

        try
        {
            var existingEvent = await eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (existingEvent is null)
            {
                throw new NotFoundException($"Event with id: {eventId} not found");
            }
            
            if (existingEvent.StartAt <= DateTime.UtcNow)
                throw new EventStartedException(
                    $"Event with id: {eventId} has already started at {existingEvent.StartAt:O}");
            
            var activeCount = await bookingRepository.CountActiveByUserAsync(userId, cancellationToken);
            if (activeCount >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Active booking limit exceeded. Limit: {MaxActiveBookingsPerUser}, current: {activeCount}");

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

    public async Task<BookingDto> CancelBookingAsync(Guid bookingId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with id: {bookingId} not found");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return mapper.Map<BookingDto>(booking);
        }

        if (booking.UserId != userId)
        {
            throw new ForbiddenException($"Booking cancellation is forbidden for user with id: {userId}");
        }

        var existingEvent = await eventRepository.GetByIdAsync(booking.EventId, cancellationToken);
        if (existingEvent is null)
        {
            throw new NotFoundException($"Event with id: {booking.EventId} not found");
        }

        if (existingEvent.StartAt <= DateTime.UtcNow)
        {
            throw new EventStartedException(
                $"Event with id: {existingEvent.Id} has already started at {existingEvent.StartAt:O}");
        }

        booking.Status = BookingStatus.Cancelled;
        existingEvent.ReleaseSeats();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<BookingDto>(booking);
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