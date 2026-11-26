using AutoMapper;
using BookingService.Application.Abstractions.Persistence.Repositories;
using BookingService.Application.Abstractions.Services;
using BookingService.Application.Dtos.Bookings;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions.Booking;
using Shared.Application.Abstractions.Persistence;
using Shared.Domain.Exceptions;

namespace BookingService.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
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
            var activeCount = await bookingRepository.CountActiveByUserAsync(userId, cancellationToken);
            if (activeCount >= MaxActiveBookingsPerUser)
                throw new BookingLimitExceededException(
                    $"Active booking limit exceeded. Limit: {MaxActiveBookingsPerUser}, current: {activeCount}");

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

    public async Task<BookingDto> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            throw new NotFoundException($"Booking with id: {bookingId} not found");
        }

        if (!isAdmin && booking.UserId != userId)
        {
            throw new ForbiddenException($"Booking cancellation is forbidden for user with id: {userId}");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return mapper.Map<BookingDto>(booking);
        }

        booking.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var data = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (data is null)
        {
            throw new NotFoundException($"Booking with id: {bookingId} not found");
        }

        if (!isAdmin && data.UserId != userId)
        {
            throw new ForbiddenException($"Access to booking {bookingId} is forbidden for user with id: {userId}");
        }

        return mapper.Map<BookingDto>(data);
    }
}