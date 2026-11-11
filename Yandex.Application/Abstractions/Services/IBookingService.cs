using Yandex.Application.Dtos.Bookings;

namespace Yandex.Application.Abstractions.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<BookingDto> CancelBookingAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
}