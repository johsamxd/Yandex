using BookingService.Application.Dtos.Bookings;

namespace BookingService.Application.Abstractions.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<BookingDto> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
}